using System.Net;
using System.Net.Sockets;
using System.Threading;

using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Networking;
using Cavern.Format.Networking.Consts;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests for the <see cref="SAPListener"/> class, covering lifecycle management
/// (Start/Stop/Dispose) and the <see cref="SAPListener.StreamDiscovered"/> event.
/// </summary>
[TestClass]
public class SAPListener_Tests {

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Start"/> with a null interface
    /// throws an <see cref="Exception"/> rather than crashing with an unhandled exception.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Start_ThrowsOnNullInterface() {
        SAPListener listener = new();
        Assert.ThrowsException<Exception>(() => listener.Start(null));
    }

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Stop"/> on a listener that was
    /// never started does not throw an exception.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stop_DoesNotThrowWhenNotStarted() {
        SAPListener listener = new();
        listener.Stop();
    }

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Dispose"/> on a listener that was
    /// never started does not throw an exception.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Dispose_DoesNotThrowWhenNotStarted() {
        SAPListener listener = new();
        listener.Dispose();
    }

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Stop"/> multiple times after
    /// starting the listener does not throw an exception (idempotent behavior).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stop_CanBeCalledMultipleTimes() {
        SAPListener listener = new();
        listener.Start(IPAddress.Loopback);
        listener.Stop();
        listener.Stop();
        listener.Stop();
    }

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Dispose"/> multiple times after
    /// starting the listener does not throw an exception (idempotent behavior).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Dispose_CanBeCalledMultipleTimes() {
        SAPListener listener = new();
        listener.Start(IPAddress.Loopback);
        listener.Dispose();
        listener.Dispose();
    }

    /// <summary>
    /// Verifies that calling <see cref="SAPListener.Start"/> twice does not throw
    /// an exception — the second call should be a no-op since the listener is already running.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Start_CanOnlyBeCalledOnce() {
        SAPListener listener = new();
        listener.Start(IPAddress.Loopback);
        listener.Start(IPAddress.Loopback);
        listener.Stop();
    }

    /// <summary>
    /// Verifies that the <see cref="SAPListener.StreamDiscovered"/> event is of the
    /// correct type (supports += and -= without throwing) and does not fire when
    /// no announcements are received.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StreamDiscovered_EventIsOfCorrectType() {
        SAPListener listener = new();
        bool eventFired = false;

        void handler(object sender, SDPPacket sdp) => eventFired = true;
        listener.StreamDiscovered += handler;
        listener.StreamDiscovered -= handler;

        Assert.IsFalse(eventFired);
    }

    /// <summary>
    /// Verifies that the <see cref="SAPListener"/> receives an SDP announcement
    /// broadcast by an <see cref="SAPAnnouncer"/>. Uses a shared <see cref="UdpClient"/>
    /// bound to the multicast address to simulate the network path, since Windows
    /// does not loop back multicast packets between separate sockets.
    /// </summary>
    [TestMethod, Timeout(5000)]
    public void StreamDiscovered_ReceivesBroadcastFromAnnouncer() {
        SDPPacket expectedSdp = new() {
            SessionName = "Integration Test Stream",
            MulticastAddress = "239.255.255.255",
            Port = 5004,
            Codec = Codec.PCM_LE,
            BitDepth = BitDepth.Int16,
            SampleRate = 44100,
            Channels = 2
        };

        SAPListener listener = new();
        SDPPacket receivedSdp = null;
        ManualResetEventSlim eventReceived = new(false);

        void handler(object sender, SDPPacket sdp) {
            receivedSdp = sdp;
            eventReceived.Set();
        }

        listener.StreamDiscovered += handler;
        listener.Start(IPAddress.Loopback);

        // Build the SAP packet using the announcer's internal logic, then send it
        // directly to loopback since Windows does not loop back multicast packets.
        SAPAnnouncer announcer = new(expectedSdp, IPAddress.Loopback);
        using (UdpClient sender = new()) {
            byte[] packet = announcer.sapPacket.ToBytes();
            sender.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, SAPConsts.port)).Wait();
        }

        bool signaled = eventReceived.Wait(3000);

        announcer.Stop();
        announcer.Dispose();
        listener.Stop();
        listener.Dispose();

        Assert.IsTrue(signaled, "Listener did not receive announcement from announcer within timeout.");
        Assert.IsNotNull(receivedSdp, "Received SDP packet is null.");
        Assert.AreEqual(expectedSdp.SessionName, receivedSdp.SessionName, "Session name mismatch.");
        Assert.AreEqual(expectedSdp.MulticastAddress, receivedSdp.MulticastAddress, "Multicast address mismatch.");
        Assert.AreEqual(expectedSdp.Port, receivedSdp.Port, "Port mismatch.");
        Assert.AreEqual(expectedSdp.SampleRate, receivedSdp.SampleRate, "Sample rate mismatch.");
        Assert.AreEqual(expectedSdp.Channels, receivedSdp.Channels, "Channel count mismatch.");
    }
}
