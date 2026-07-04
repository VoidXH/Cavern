using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Cavern.Format.Networking.Consts;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Periodically broadcasts SAP (Session Announcement Protocol) messages containing the SDP description of an audio stream to the multicast group.
    /// </summary>
    public class SAPAnnouncer : IDisposable {
        /// <summary>
        /// The pre-built SAP packet used for each announcement.
        /// </summary>
         public readonly SAPPacket sapPacket;

        /// <summary>
        /// The UDP client used to send SAP announcements.
        /// </summary>
        readonly UdpClient udpClient;

        /// <summary>
        /// The SDP message describing the audio stream to announce.
        /// </summary>
        readonly SDPPacket sdpPacket;

        /// <summary>
        /// The IPEndPoint representing the SAP multicast destination.
        /// </summary>
        readonly IPEndPoint endpoint;

        /// <summary>
        /// The local network interface IP address.
        /// </summary>
        readonly IPAddress localInterface;

        /// <summary>
        /// The cancellation token source for stopping the announcer.
        /// </summary>
        CancellationTokenSource cts;

        /// <summary>
        /// The task responsible for broadcasting SAP announcements at regular intervals.
        /// </summary>
        Task announceTask;

        /// <summary>
        /// Creates a new <see cref="SAPAnnouncer"/> for the given SDP message on the local IP address.
        /// </summary>
        /// <param name="sdpPacket">The SDP message to include in SAP announcements.</param>
        public SAPAnnouncer(SDPPacket sdpPacket) : this(sdpPacket, GetLocalIPAddress()) { }

        /// <summary>
        /// Creates a new <see cref="SAPAnnouncer"/> for the given SDP message.
        /// </summary>
        /// <param name="sdpPacket">The SDP message to include in SAP announcements.</param>
        /// <param name="localInterface">Select a local network by interface IP address.</param>
        public SAPAnnouncer(SDPPacket sdpPacket, IPAddress localInterface) {
            this.sdpPacket = sdpPacket;
            this.localInterface = localInterface;
            udpClient = new UdpClient();
            if (localInterface != null) {
                udpClient.JoinMulticastGroup(IPAddress.Parse(SAPConsts.multicastAddress), localInterface);
            } else {
                udpClient.JoinMulticastGroup(IPAddress.Parse(SAPConsts.multicastAddress));
            }
            endpoint = new IPEndPoint(IPAddress.Parse(SAPConsts.multicastAddress), SAPConsts.port);
            sapPacket = BuildSAPPacket();
        }

        /// <summary>
        /// Retrieves the first available IPv4 address from the local machine's network interfaces, excluding loopback addresses.
        /// </summary>
        /// <returns>The first non-loopback IPv4 address found, or <see cref="IPAddress.Loopback"/> if none exists.</returns>
        static IPAddress GetLocalIPAddress() {
            try {
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) {
                        return ip;
                    }
                }
            } catch {
                // Fall back to loopback on resolution issues
            }
            return IPAddress.Loopback;
        }

        /// <summary>
        /// Starts broadcasting SAP announcements at the specified interval.
        /// If already running, this call is ignored.
        /// </summary>
        /// <param name="interval">The time interval between announcements.</param>
        public void Start(TimeSpan interval) {
            if (cts != null) {
                return;
            }

            cts = new CancellationTokenSource();
            announceTask = AnnounceLoopAsync(interval, cts.Token);
        }

        /// <summary>
        /// Stops the SAP announcer, cancels the announce task, and disposes resources.
        /// </summary>
        public void Stop() {
            cts?.Cancel();
            try {
                announceTask?.Wait();
            } catch {
                // If runs to exception, it's over anyway
            }
            cts?.Dispose();
            cts = null;
        }

        /// <summary>
        /// Stops the announcer and releases all resources.
        /// </summary>
        public void Dispose() {
            GC.SuppressFinalize(this);
            Stop();
            udpClient.Dispose();
        }

        /// <summary>
        /// Builds a <see cref="SAPPacket"/> from the stored SDP message and local interface address.
        /// </summary>
        /// <returns>A configured <see cref="SAPPacket"/> ready for serialization.</returns>
        SAPPacket BuildSAPPacket() {
            byte[] sdpBytes = Encoding.UTF8.GetBytes(sdpPacket.ToString());

            return new SAPPacket {
                Version = 1,
                Delete = false,
                Encrypted = false,
                Compressed = false,
                MsgIdHash = (ushort)new Random().Next(),
                OriginatingSource = localInterface,
                AuthenticationData = Array.Empty<byte>(),
                PayloadType = "application/sdp",
                Payload = sdpBytes
            };
        }

        /// <summary>
        /// Continuously sends SAP announcement packets at the specified interval.
        /// Each packet is serialized from the <see cref="SAPPacket"/> instance.
        /// </summary>
        /// <param name="interval">The time interval between announcements.</param>
        /// <param name="token">Token used to cancel the announce loop.</param>
        async Task AnnounceLoopAsync(TimeSpan interval, CancellationToken token) {
            byte[] packet = sapPacket.ToBytes();
            while (!token.IsCancellationRequested) {
                try {
                    await udpClient.SendAsync(packet, packet.Length, endpoint);
                    await Task.Delay(interval, token);
                } catch (TaskCanceledException) {
                    break;
                } catch {
                    // Don't crash the loop on send errors, just try again
                }
            }
        }
    }
}
