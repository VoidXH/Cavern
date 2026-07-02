using Cavern.Format.Networking;
using Cavern.Format.Networking.Exceptions;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests the <see cref="RTPPacket"/> class.
/// </summary>
[TestClass]
public class RtpPacket_Tests {
    /// <summary>
    /// Tests if the constructor initializes all properties to their default values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_InitializesDefaults() {
        RTPPacket packet = new RTPPacket();
        Assert.AreEqual(2, packet.Version);
        Assert.IsFalse(packet.Padding);
        Assert.IsFalse(packet.Extension);
        Assert.AreEqual(0, packet.CSRCCount);
        Assert.IsFalse(packet.Marker);
        Assert.AreEqual(0, packet.SequenceNumber);
        Assert.AreEqual((uint)0, packet.Timestamp);
        Assert.AreEqual((uint)0, packet.SSRC);
        Assert.AreEqual(0, packet.Payload.Length);
    }

    /// <summary>
    /// Tests if ToBytes produces a byte array that parses back to the original packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ParsesBackCorrectly() {
        RTPPacket original = new RTPPacket {
            PayloadType = 97,
            SequenceNumber = 1234,
            Timestamp = 56789,
            SSRC = 0xDEADBEEF,
            Payload = [0x01, 0x02, 0x03, 0x04]
        };

        byte[] bytes = original.ToBytes();
        RTPPacket parsed = RTPPacket.Parse(bytes);
        Assert.AreEqual(2, parsed.Version);
        Assert.IsFalse(parsed.Padding);
        Assert.IsFalse(parsed.Extension);
        Assert.AreEqual(0, parsed.CSRCCount);
        Assert.IsFalse(parsed.Marker);
        Assert.AreEqual(97, parsed.PayloadType);
        Assert.AreEqual(1234, parsed.SequenceNumber);
        Assert.AreEqual((uint)56789, parsed.Timestamp);
        Assert.AreEqual(0xDEADBEEF, parsed.SSRC);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, parsed.Payload);
    }

    /// <summary>
    /// Tests if ToBytes includes padding and extension flags correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_IncludesPaddingAndExtensionFlags() {
        RTPPacket original = new RTPPacket {
            Version = 2,
            Padding = true,
            Extension = false,
            CSRCCount = 0,
            Marker = true,
            PayloadType = 100,
            SequenceNumber = 0xFFFF,
            Timestamp = 0x12345678,
            SSRC = 0xABCDEF00,
            Payload = [0xAA]
        };

        byte[] bytes = original.ToBytes();
        RTPPacket parsed = RTPPacket.Parse(bytes);
        Assert.IsTrue(parsed.Padding);
        Assert.IsFalse(parsed.Extension);
        Assert.AreEqual(0, parsed.CSRCCount);
        Assert.IsTrue(parsed.Marker);
        Assert.AreEqual(100, parsed.PayloadType);
        Assert.AreEqual(0xFFFF, parsed.SequenceNumber);
        Assert.AreEqual((uint)0x12345678, parsed.Timestamp);
        Assert.AreEqual(0xABCDEF00, parsed.SSRC);
    }

    /// <summary>
    /// Tests if ToBytes returns the correct header size for a packet without CSRC or extension.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ReturnsCorrectHeaderSize() {
        RTPPacket packet = new RTPPacket {
            PayloadType = 96,
            Payload = []
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(12, bytes.Length);
    }

    /// <summary>
    /// Tests if ToBytes preserves an empty payload.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_PreservesEmptyPayload() {
        RTPPacket packet = new RTPPacket {
            PayloadType = 96,
            SequenceNumber = 1,
            Payload = []
        };

        byte[] bytes = packet.ToBytes();
        RTPPacket parsed = RTPPacket.Parse(bytes);
        Assert.AreEqual(0, parsed.Payload.Length);
    }

    /// <summary>
    /// Tests if Parse throws an InvalidPacketException when the data is too small.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_ThrowsOnTooSmallData() {
        byte[] smallData = [0x80, 0x60];
        Assert.ThrowsException<InvalidPacketException>(() => RTPPacket.Parse(smallData));
    }

    /// <summary>
    /// Tests if Parse correctly handles CSRC count.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_HandlesCsrcCount() {
        byte[] data = [
            0x23, 0x60, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44, 0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
        ];
        RTPPacket packet = RTPPacket.Parse(data);

        Assert.AreEqual(3, packet.CSRCCount);
        Assert.AreEqual(0, packet.Payload.Length);
    }

    /// <summary>
    /// Tests if Parse correctly handles the extension header.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_HandlesExtensionHeader() {
        byte[] data = [
            0x90, 0x60, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44, 0x12, 0x34, 0x00, 0x01,
            0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04,
        ];
        RTPPacket packet = RTPPacket.Parse(data);

        Assert.IsTrue(packet.Extension);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, packet.Payload);
    }

    /// <summary>
    /// Tests if Parse correctly handles extension with CSRC entries.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_HandlesExtensionWithCsrc() {
        byte[] data = [
            0x91, 0x60, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x00, 0x11, 0x00, 0x01,
            0xDE, 0xAD, 0xBE, 0xEF,
            0x01, 0x02, 0x03, 0x04,
        ];
        RTPPacket packet = RTPPacket.Parse(data);

        Assert.AreEqual(1, packet.CSRCCount);
        Assert.IsTrue(packet.Extension);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, packet.Payload);
    }

    /// <summary>
    /// Tests if Parse correctly locates the payload after CSRC and extension data.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_PayloadStartsAfterCsrcAndExtension() {
        byte[] data = [
            0x92, 0x7F, 0x00, 0x42, 0x00, 0x00, 0x00, 0xCA, 0xFF, 0xFF, 0xFF, 0xFF,
            0x11, 0x11, 0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0xAB, 0xCD, 0x00, 0x01,
            0x01, 0x02, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
        ];

        RTPPacket packet = RTPPacket.Parse(data);
        Assert.AreEqual(2, packet.CSRCCount);
        Assert.IsTrue(packet.Extension);
        Assert.AreEqual(127, packet.PayloadType);
        Assert.AreEqual(66, packet.SequenceNumber);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, packet.Payload);
    }
}
