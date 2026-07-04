using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Cavern.Format.Networking.Consts;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Listens for SAP (Session Announcement Protocol) messages on the multicast group and raises the <see cref="StreamDiscovered"/> event when a valid SDP announcement is received.
    /// </summary>
    public class SAPListener : IDisposable {
        /// <summary>
        /// Event raised when a valid SDP announcement is received and parsed.
        /// </summary>
        public event EventHandler<SDPPacket> StreamDiscovered;

        /// <summary>
        /// The UDP client used to receive SAP announcements.
        /// </summary>
        UdpClient udpClient;

        /// <summary>
        /// The cancellation token source for stopping the listener.
        /// </summary>
        CancellationTokenSource cts;

        /// <summary>
        /// The task responsible for listening to SAP announcements.
        /// </summary>
        Task listenTask;

        /// <summary>
        /// Starts listening for SAP announcements on the specified local network interface. Binds to the SAP multicast port and joins the multicast group.
        /// </summary>
        /// <param name="localInterface">The local network interface to bind to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="localInterface"/> is null.</exception>
        public void Start(IPAddress localInterface) {
            if (cts != null) {
                return;
            }

            if (localInterface == null) {
                throw new Exception("Local interface cannot be null.");
            }

            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
             udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
             udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, SAPConsts.port));
             udpClient.JoinMulticastGroup(IPAddress.Parse(SAPConsts.multicastAddress), localInterface);

            cts = new CancellationTokenSource();
            listenTask = ListenLoopAsync(cts.Token);
        }

        /// <summary>
        /// Stops the SAP listener, cancels the listen task, and leaves the multicast group.
        /// Safe to call multiple times.
        /// </summary>
        public void Stop() {
            CancellationTokenSource localCts = cts;
            if (localCts == null) {
                return;
            }

            localCts.Cancel();
            try {
                listenTask?.Wait();
            } catch {
                // The task is stopped anyway on errors, just skip them
            }

            UdpClient localClient = udpClient;
            if (localClient != null) {
                try {
                    localClient.DropMulticastGroup(IPAddress.Parse(SAPConsts.multicastAddress));
                } catch {
                    // DropMulticastGroup may fail if the client was already closed by the cancellation token registration callback
                }
                localClient.Close();
            }

            udpClient = null;
            cts = null;
        }

        /// <inheritdoc/>
        public void Dispose() {
            GC.SuppressFinalize(this);
            Stop();
        }

        /// <summary>
        /// Continuously listens for SAP announcements, parses SDP payloads, and raises <see cref="StreamDiscovered"/> for each valid announcement.
        /// </summary>
        /// <param name="token">Token used to cancel the listen loop.</param>
        async Task ListenLoopAsync(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    if (udpClient == null) {
                        break;
                    }

                    UdpReceiveResult result;
                    using (cts.Token.Register(() => udpClient.Close())) {
                        try {
                            result = await udpClient.ReceiveAsync();
                        } catch (ObjectDisposedException) when (cts.Token.IsCancellationRequested) {
                            break;
                        }
                    }
                    byte[] data = result.Buffer;

                    if (data.Length < 8) {
                        continue;
                    }

                    // Skip basic SAP header (RFC 2974)
                    int authLen = data[1] * 4; // Auth length is in 32-bit words
                    int ipAddrLen = (data[0] & 0x10) != 0 ? 16 : 4; // IPv6 vs IPv4 check
                    int headerLen = 4 + ipAddrLen + authLen;

                    if (data.Length < headerLen) {
                        continue;
                    }

                    // Find MIME null terminator
                    int mimeEnd = -1;
                    for (int i = headerLen; i < data.Length; i++) {
                        if (data[i] == 0) {
                            mimeEnd = i;
                            break;
                        }
                    }

                    if (mimeEnd == -1) {
                        continue;
                    }

                    string mime = Encoding.ASCII.GetString(data, headerLen, mimeEnd - headerLen);
                    if (mime == "application/sdp") {
                        int payloadStart = mimeEnd + 1;
                        string sdpStr = Encoding.UTF8.GetString(data, payloadStart, data.Length - payloadStart);

                        SDPPacket sdpMessage = SDPPacket.Parse(sdpStr);
                        StreamDiscovered?.Invoke(this, sdpMessage);
                    }
                } catch (OperationCanceledException) {
                    break;
                } catch {
                    // Ignore parsing errors
                }
            }
        }
    }
}
