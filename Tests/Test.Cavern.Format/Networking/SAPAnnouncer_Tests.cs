using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Networking;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests the <see cref="SAPAnnouncer"/> class.
/// </summary>
[TestClass]
public class SAPAnnouncer_Tests {
    /// <summary>
    /// Tests if the constructor initializes with an SDP message.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_InitializesWithSdpMessage() {
        SDPPacket sdp = new() {
            SessionName = "Test Stream",
            MulticastAddress = "239.69.0.1",
            Port = 5004
        };

        SAPAnnouncer announcer = new(sdp);
        Assert.IsNotNull(announcer);
    }

    /// <summary>
    /// Tests if Stop does not throw when the announcer has not been started.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stop_DoesNotThrowWhenNotStarted() {
        SDPPacket sdp = new();
        SAPAnnouncer announcer = new(sdp);

        announcer.Stop();
    }

    /// <summary>
    /// Tests if Dispose does not throw when the announcer has not been started.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Dispose_DoesNotThrowWhenNotStarted() {
        SDPPacket sdp = new();
        SAPAnnouncer announcer = new(sdp);
        announcer.Dispose();
    }

    /// <summary>
    /// Tests if Stop can be called multiple times without throwing.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stop_CanBeCalledMultipleTimes() {
        SDPPacket sdp = new();
        SAPAnnouncer announcer = new(sdp);
        announcer.Stop();
        announcer.Stop();
    }

    /// <summary>
    /// Tests if Dispose can be called multiple times without throwing.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Dispose_CanBeCalledMultipleTimes() {
        SDPPacket sdp = new();
        SAPAnnouncer announcer = new(sdp);
        announcer.Dispose();
        announcer.Dispose();
    }

    /// <summary>
    /// Tests if Start can only be called once (subsequent calls are no-ops).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Start_CanOnlyBeCalledOnce() {
        SDPPacket sdp = new();
        SAPAnnouncer announcer = new(sdp);
        announcer.Start(TimeSpan.FromMilliseconds(10));
        announcer.Start(TimeSpan.FromMilliseconds(10)); // Second call should be a no-op
        announcer.Stop();
    }

    /// <summary>
    /// Tests if the constructor stores the SDP message correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_StoresSdpMessage() {
        SDPPacket sdp = new() {
            SessionName = "Unique Stream Name",
            MulticastAddress = "239.100.0.1",
            Port = 6000,
            Codec = Codec.PCM_LE,
            BitDepth = BitDepth.Int16,
            SampleRate = 44100,
            Channels = 2
        };

        SAPAnnouncer announcer = new(sdp);
        Assert.IsNotNull(announcer);
    }
}
