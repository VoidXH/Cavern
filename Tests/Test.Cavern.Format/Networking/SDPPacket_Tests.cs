using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Networking;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests the <see cref="SDPPacket"/> class.
/// </summary>
[TestClass]
public class SdpMessage_Tests {
    /// <summary>
    /// Tests if the constructor initializes all properties to their default values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_InitializesDefaultValues() {
        SDPPacket sdp = new SDPPacket();

        Assert.AreEqual("Cavern", sdp.SessionName);
        Assert.AreEqual("-", sdp.Originator);
        Assert.AreEqual("239.69.0.1", sdp.MulticastAddress);
        Assert.AreEqual(5004, sdp.Port);
        Assert.AreEqual(97, sdp.PayloadType);
        Assert.AreEqual(Codec.PCM_LE, sdp.Codec);
        Assert.AreEqual(BitDepth.Int24, sdp.BitDepth);
        Assert.AreEqual(48000, sdp.SampleRate);
        Assert.AreEqual(2, sdp.Channels);
        Assert.AreEqual(1, sdp.PacketTime);
    }

    /// <summary>
    /// Tests if ToString produces a valid SDP-formatted string.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToString_ReturnsValidSdpFormat() {
        SDPPacket sdp = new SDPPacket {
            SessionName = "Test Stream",
            Originator = "user1",
            SessionID = 12345,
            SessionVersion = 67890,
            MulticastAddress = "239.69.0.2",
            Port = 5006,
            PayloadType = 98,
            Codec = Codec.PCM_LE,
            BitDepth = BitDepth.Int16,
            SampleRate = 44100,
            Channels = 2,
            PacketTime = 2
        };

        string output = sdp.ToString();
        StringAssert.Contains(output, "v=0");
        StringAssert.Contains(output, "o=user1 12345 67890 IN IP4 239.69.0.2");
        StringAssert.Contains(output, "s=Test Stream");
        StringAssert.Contains(output, "c=IN IP4 239.69.0.2/32");
        StringAssert.Contains(output, "t=0 0");
        StringAssert.Contains(output, "m=audio 5006 RTP/AVP 98");
        StringAssert.Contains(output, "a=rtpmap:98 L16/44100/2");
        StringAssert.Contains(output, "a=ptime:2");
        StringAssert.Contains(output, "a=ts-refclk:ptp=IEEE1588-2008:00-00-00-00-00-00-00-00:0");
        StringAssert.Contains(output, "a=mediaclk:direct=0");
    }

    /// <summary>
    /// Tests if Parse correctly parses a standard SDP message.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_ParsesStandardSdpCorrectly() {
        string input = @"v=0
o=- 12345 67890 IN IP4 239.69.1.1
s=Live Audio
c=IN IP4 239.69.1.1/32
t=0 0
m=audio 5004 RTP/AVP 97
a=rtpmap:97 L24/48000/2
a=ptime:1
a=ts-refclk:ptp=IEEE1588-2008:00-00-00-00-00-00-00-00:0
a=mediaclk:direct=0";

        SDPPacket sdp = SDPPacket.Parse(input);
        Assert.AreEqual("Live Audio", sdp.SessionName);
        Assert.AreEqual("-", sdp.Originator);
        Assert.AreEqual(12345, sdp.SessionID);
        Assert.AreEqual(67890, sdp.SessionVersion);
        Assert.AreEqual("239.69.1.1", sdp.MulticastAddress);
        Assert.AreEqual(5004, sdp.Port);
        Assert.AreEqual(97, sdp.PayloadType);
        Assert.AreEqual(Codec.PCM_LE, sdp.Codec);
        Assert.AreEqual(BitDepth.Int24, sdp.BitDepth);
        Assert.AreEqual(48000, sdp.SampleRate);
        Assert.AreEqual(2, sdp.Channels);
        Assert.AreEqual("ptp", sdp.ClockSource);
        Assert.AreEqual("00-00-00-00-00-00-00-00", sdp.PTP_GMID);
        Assert.AreEqual(0, sdp.PTPDomain);
        Assert.AreEqual(0L, sdp.MediaClockOffset);
    }

    /// <summary>
    /// Tests if Parse correctly handles different codec settings.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_HandlesDifferentCodecSettings() {
        string input = @"v=0
o=tester 100 200 IN IP4 239.0.0.1
s=Test
c=IN IP4 239.0.0.1/32
t=0 0
m=audio 6000 RTP/AVP 96
a=rtpmap:96 L16/44100/1
a=ptime:2";

        SDPPacket sdp = SDPPacket.Parse(input);
        Assert.AreEqual("Test", sdp.SessionName);
        Assert.AreEqual("239.0.0.1", sdp.MulticastAddress);
        Assert.AreEqual(6000, sdp.Port);
        Assert.AreEqual(96, sdp.PayloadType);
        Assert.AreEqual(Codec.PCM_LE, sdp.Codec);
        Assert.AreEqual(BitDepth.Int16, sdp.BitDepth);
        Assert.AreEqual(44100, sdp.SampleRate);
        Assert.AreEqual(1, sdp.Channels);
        Assert.AreEqual(2, sdp.PacketTime);
    }

    /// <summary>
    /// Tests if Parse correctly handles missing optional fields.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_HandlesMissingOptionalFields() {
        string input = @"v=0
o=- 1 2 IN IP4 239.0.0.5
s=Minimal
c=IN IP4 239.0.0.5/32
t=0 0
m=audio 5000 RTP/AVP 97";

        SDPPacket sdp = SDPPacket.Parse(input);
        Assert.AreEqual("Minimal", sdp.SessionName);
        Assert.AreEqual("239.0.0.5", sdp.MulticastAddress);
        Assert.AreEqual(5000, sdp.Port);
        Assert.AreEqual(97, sdp.PayloadType);
        Assert.AreEqual(Codec.PCM_LE, sdp.Codec);
        Assert.AreEqual(BitDepth.Int24, sdp.BitDepth);
        Assert.AreEqual(48000, sdp.SampleRate);
        Assert.AreEqual(2, sdp.Channels);
        Assert.AreEqual(1, sdp.PacketTime);
    }

    /// <summary>
    /// Tests if ToString and Parse produce consistent results in a round-trip.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToString_RoundTripWithParse() {
        SDPPacket original = new SDPPacket {
            SessionName = "RoundTrip Test",
            Originator = "tester",
            SessionID = 999,
            SessionVersion = 1000,
            MulticastAddress = "239.100.0.1",
            Port = 7000,
            PayloadType = 99,
            Codec = Codec.PCM_LE,
            BitDepth = BitDepth.Int16,
            SampleRate = 96000,
            Channels = 8,
            PacketTime = 1,
            ClockSource = "ptp",
            PTP_GMID = "00-11-22-33-44-55-66-77",
            PTPDomain = 4,
            MediaClockOffset = 123456L
        };

        string serialized = original.ToString();
        SDPPacket parsed = SDPPacket.Parse(serialized);
        Assert.AreEqual(original.SessionName, parsed.SessionName);
        Assert.AreEqual(original.MulticastAddress, parsed.MulticastAddress);
        Assert.AreEqual(original.Port, parsed.Port);
        Assert.AreEqual(original.PayloadType, parsed.PayloadType);
        Assert.AreEqual(original.Codec, parsed.Codec);
        Assert.AreEqual(original.BitDepth, parsed.BitDepth);
        Assert.AreEqual(original.SampleRate, parsed.SampleRate);
        Assert.AreEqual(original.Channels, parsed.Channels);
        Assert.AreEqual(original.ClockSource, parsed.ClockSource);
        Assert.AreEqual(original.PTP_GMID, parsed.PTP_GMID);
        Assert.AreEqual(original.PTPDomain, parsed.PTPDomain);
        Assert.AreEqual(original.MediaClockOffset, parsed.MediaClockOffset);
    }
}
