using System.Net;
using System.Text;

using Cavern.Format.Networking;
using Cavern.Format.Networking.Exceptions;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests the <see cref="SAPPacket"/> class.
/// </summary>
[TestClass]
public class SAPPacket_Tests {
    /// <summary>
    /// Tests if the default constructor initializes all properties to their expected default values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Default_Constructor_SetsExpectedDefaults() {
        SAPPacket packet = new();
        Assert.AreEqual(1, packet.Version);
        Assert.IsFalse(packet.Delete);
        Assert.IsFalse(packet.Encrypted);
        Assert.IsFalse(packet.Compressed);
        Assert.AreEqual(0x0420, packet.MsgIdHash);
        Assert.AreEqual(IPAddress.Loopback, packet.OriginatingSource);
        Assert.AreEqual(0, packet.AuthenticationData.Length);
        Assert.AreEqual("application/sdp", packet.PayloadType);
        Assert.AreEqual(0, packet.Payload.Length);
    }

    /// <summary>
    /// Tests if all properties can be set and read back correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Properties_CanBeSetAndRead() {
        SAPPacket packet = new() {
            Version = 1,
            Delete = true,
            Encrypted = true,
            Compressed = true,
            MsgIdHash = 0x1234,
            OriginatingSource = IPAddress.Parse("192.168.1.1"),
            AuthenticationData = [0x01, 0x02, 0x03, 0x04],
            PayloadType = "application/custom",
            Payload = [0xDE, 0xAD, 0xBE, 0xEF]
        };

        Assert.AreEqual(1, packet.Version);
        Assert.IsTrue(packet.Delete);
        Assert.IsTrue(packet.Encrypted);
        Assert.IsTrue(packet.Compressed);
        Assert.AreEqual(0x1234, packet.MsgIdHash);
        Assert.AreEqual(IPAddress.Parse("192.168.1.1"), packet.OriginatingSource);
        Assert.AreEqual(4, packet.AuthenticationData.Length);
        Assert.AreEqual("application/custom", packet.PayloadType);
        Assert.AreEqual(4, packet.Payload.Length);
    }

    /// <summary>
    /// Tests that ToBytes produces a valid IPv4 packet with correct header fields.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ProducesValidIPv4Packet() {
        SAPPacket packet = new() {
            Version = 1,
            OriginatingSource = IPAddress.Parse("192.168.1.1"),
            PayloadType = "application/sdp",
            Payload = Encoding.UTF8.GetBytes("v=0\r\no=test 1 1 IN IP4 239.69.0.1")
        };

        byte[] bytes = packet.ToBytes();
        Assert.IsTrue(bytes.Length > 0);
        Assert.IsTrue(bytes.Length >= 8);
        // Byte 0: version bits (3 bits) at top
        byte flags = bytes[0];
        byte version = (byte)((flags >> 5) & 0x07);
        Assert.AreEqual(1, version);
        // Auth length in byte 1 should be 0 (no auth data)
        Assert.AreEqual(0, bytes[1]);
    }

    /// <summary>
    /// Tests that ToBytes produces a valid IPv6 packet with the IPv6 flag set.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ProducesValidIPv6Packet() {
        SAPPacket packet = new() {
            Version = 1,
            OriginatingSource = IPAddress.Parse("::1"),
            PayloadType = "application/sdp",
            Payload = [0x01, 0x02]
        };

        byte[] bytes = packet.ToBytes();
        Assert.IsTrue(bytes.Length > 0);
        // Byte 0: IPv6 flag bit (bit 4) should be set
        byte flags = bytes[0];
        Assert.IsTrue((flags & 0x10) != 0, "IPv6 flag should be set");
        // Auth length should be 0
        Assert.AreEqual(0, bytes[1]);
    }

    /// <summary>
    /// Tests that ToBytes encodes the MsgIdHash correctly in big-endian format.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ProducesCorrectMsgIdHash() {
        SAPPacket packet = new() {
            MsgIdHash = 0x1234,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = packet.ToBytes();
        // MsgIdHash is at bytes 2-3, big-endian
        Assert.AreEqual(0x12, bytes[2]);
        Assert.AreEqual(0x34, bytes[3]);
    }

    /// <summary>
    /// Tests that the Delete flag is correctly encoded in the packet bytes.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithDeleteFlag_SetsDeleteBit() {
        SAPPacket packet = new() {
            Delete = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = packet.ToBytes();
        Assert.IsTrue((bytes[0] & 0x04) != 0, "Delete bit should be set"); // Delete bit is bit 2 of byte 0
    }

    /// <summary>
    /// Tests that the Encrypted flag is correctly encoded in the packet bytes.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithEncryptedFlag_SetsEncryptedBit() {
        SAPPacket packet = new() {
            Encrypted = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = packet.ToBytes();
        Assert.IsTrue((bytes[0] & 0x02) != 0, "Encrypted bit should be set"); // Encrypted bit is bit 1 of byte 0
    }

    /// <summary>
    /// Tests that the Compressed flag is correctly encoded in the packet bytes.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithCompressedFlag_SetsCompressedBit() {
        SAPPacket packet = new() {
            Compressed = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = packet.ToBytes();
        Assert.IsTrue((bytes[0] & 0x01) != 0, "Compressed bit should be set"); // Compressed bit is bit 0 of byte 0
    }

    /// <summary>
    /// Tests that authentication data shorter than 4 bytes is padded to a multiple of 4.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithAuthenticationData_PadsToMultipleOf4() {
        SAPPacket packet = new() {
            OriginatingSource = IPAddress.Loopback,
            AuthenticationData = [0x01, 0x02, 0x03] // 3 bytes, should pad to 4
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(1, bytes[1]); // Auth length in byte 1 should be 1 (1 word = 4 bytes)
    }

    /// <summary>
    /// Tests that authentication data already a multiple of 4 bytes is not over-padded.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithAuthenticationData_32Bytes_PadsToMultipleOf4() {
        SAPPacket packet = new() {
            OriginatingSource = IPAddress.Loopback,
            AuthenticationData = new byte[32] // Already a multiple of 4
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(8, bytes[1]); // Auth length in byte 1 should be 8 (8 words = 32 bytes)
    }

    /// <summary>
    /// Tests that the payload type is encoded with a null terminator after the header.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ContainsPayloadTypeWithNullTerminator() {
        SAPPacket packet = new() {
            OriginatingSource = IPAddress.Loopback,
            PayloadType = "application/sdp",
            Payload = [0x01]
        };

        byte[] bytes = packet.ToBytes();

        // Find the null terminator after the header
        int authLen = bytes[1];
        int ipAddrLen = 4; // IPv4
        int headerLen = 4 + ipAddrLen + authLen * 4;
        int mimeStart = headerLen;

        // Find null terminator
        int mimeEnd = -1;
        for (int i = mimeStart; i < bytes.Length; i++) {
            if (bytes[i] == 0) {
                mimeEnd = i;
                break;
            }
        }

        Assert.IsTrue(mimeEnd >= 0, "MIME null terminator should be found");
        string mime = Encoding.ASCII.GetString(bytes, mimeStart, mimeEnd - mimeStart);
        Assert.AreEqual("application/sdp", mime);
    }

    /// <summary>
    /// Tests that the payload is correctly placed after the MIME null terminator.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_ContainsPayloadAfterMimeNullTerminator() {
        byte[] expectedPayload = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04];
        SAPPacket packet = new() {
            OriginatingSource = IPAddress.Loopback,
            Payload = expectedPayload
        };

        byte[] bytes = packet.ToBytes();

        // Find the null terminator
        int authLen = bytes[1];
        int ipAddrLen = 4;
        int headerLen = 4 + ipAddrLen + authLen * 4;
        int mimeStart = headerLen;

        int mimeEnd = -1;
        for (int i = mimeStart; i < bytes.Length; i++) {
            if (bytes[i] == 0) {
                mimeEnd = i;
                break;
            }
        }

        int payloadStart = mimeEnd + 1;
        byte[] actualPayload = new byte[bytes.Length - payloadStart];
        Buffer.BlockCopy(bytes, payloadStart, actualPayload, 0, actualPayload.Length);

        CollectionAssert.AreEqual(expectedPayload, actualPayload);
    }

    /// <summary>
    /// Tests that Parse correctly decodes a valid IPv4 packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithValidIPv4Packet_ReturnsCorrectValues() {
        SAPPacket original = new() {
            Version = 1,
            OriginatingSource = IPAddress.Parse("192.168.1.100"),
            MsgIdHash = 0xABCD,
            PayloadType = "application/sdp",
            Payload = Encoding.UTF8.GetBytes("test payload")
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(1, parsed.Version);
        Assert.AreEqual(IPAddress.Parse("192.168.1.100"), parsed.OriginatingSource);
        Assert.AreEqual(0xABCD, parsed.MsgIdHash);
        Assert.AreEqual("application/sdp", parsed.PayloadType);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("test payload"), parsed.Payload);
    }

    /// <summary>
    /// Tests that Parse correctly decodes a valid IPv6 packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithValidIPv6Packet_ReturnsCorrectValues() {
        SAPPacket original = new() {
            Version = 1,
            OriginatingSource = IPAddress.Parse("2001:db8::1"),
            MsgIdHash = 0x5678,
            PayloadType = "application/sdp",
            Payload = [0x01, 0x02, 0x03]
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(1, parsed.Version);
        Assert.AreEqual(IPAddress.Parse("2001:db8::1"), parsed.OriginatingSource);
        Assert.AreEqual(0x5678, parsed.MsgIdHash);
        Assert.AreEqual("application/sdp", parsed.PayloadType);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, parsed.Payload);
    }

    /// <summary>
    /// Tests that the Delete flag is correctly parsed from the packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithDeleteFlag_ReturnsDeleteTrue() {
        SAPPacket original = new() {
            Delete = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.IsTrue(parsed.Delete);
    }

    /// <summary>
    /// Tests that the Encrypted flag is correctly parsed from the packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithEncryptedFlag_ReturnsEncryptedTrue() {
        SAPPacket original = new() {
            Encrypted = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.IsTrue(parsed.Encrypted);
    }

    /// <summary>
    /// Tests that the Compressed flag is correctly parsed from the packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithCompressedFlag_ReturnsCompressedTrue() {
        SAPPacket original = new() {
            Compressed = true,
            OriginatingSource = IPAddress.Loopback
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.IsTrue(parsed.Compressed);
    }

    /// <summary>
    /// Tests that authentication data is correctly parsed from the packet.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithAuthenticationData_ReturnsAuthData() {
        byte[] authData = [0xAA, 0xBB, 0xCC, 0xDD];
        SAPPacket original = new() {
            OriginatingSource = IPAddress.Loopback,
            AuthenticationData = authData
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        CollectionAssert.AreEqual(authData, parsed.AuthenticationData);
    }

    /// <summary>
    /// Tests that an empty payload is correctly parsed.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithEmptyPayload_ReturnsEmptyPayload() {
        SAPPacket original = new() {
            OriginatingSource = IPAddress.Loopback,
            Payload = []
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(0, parsed.Payload.Length);
    }

    /// <summary>
    /// Tests that a custom payload type is correctly parsed.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithCustomPayloadType_ReturnsCorrectPayloadType() {
        SAPPacket original = new() {
            OriginatingSource = IPAddress.Loopback,
            PayloadType = "application/custom-type"
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual("application/custom-type", parsed.PayloadType);
    }

    /// <summary>
    /// Tests that Parse throws InvalidPacketException when the packet is too small.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_ThrowsOnTooSmallPacket() {
        byte[] tooSmall = [0x20, 0x00];
        Assert.ThrowsException<InvalidPacketException>(() => SAPPacket.Parse(tooSmall));
    }

    /// <summary>
    /// Tests that Parse throws InvalidPacketException when the packet is too small for the header.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_ThrowsOnPacketTooSmallForHeader() {
        // IPv4 header needs at least 8 bytes (4 + 4)
        byte[] tooSmall = [0x20, 0x00, 0x00, 0x01];
        Assert.ThrowsException<InvalidPacketException>(() => SAPPacket.Parse(tooSmall));
    }

    /// <summary>
    /// Tests that Parse throws InvalidPacketException when no MIME null terminator is found.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_ThrowsOnMissingMimeNullTerminator() {
        // Build a packet with enough bytes for header but no null terminator
        byte[] data = new byte[20];
        data[0] = 0x20; // Version 1, no flags
        data[1] = 0x00; // Auth len 0
        data[2] = 0x00; // MsgIdHash
        data[3] = 0x01;
        // Fill IP address
        data[4] = 192;
        data[5] = 168;
        data[6] = 1;
        data[7] = 1;
        // Fill remaining bytes with non-zero values so no null terminator exists
        for (int i = 8; i < data.Length; i++) {
            data[i] = 0xFF;
        }

        Assert.ThrowsException<InvalidPacketException>(() => SAPPacket.Parse(data));
    }

    /// <summary>
    /// Tests that all flags are correctly parsed when set.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithAllFlagsSet_ParsesCorrectly() {
        SAPPacket original = new SAPPacket {
            Version = 1,
            Delete = true,
            Encrypted = true,
            Compressed = true,
            MsgIdHash = 0xFFFF,
            OriginatingSource = IPAddress.Parse("10.0.0.1"),
            AuthenticationData = [0x01, 0x02, 0x03, 0x04],
            PayloadType = "application/sdp",
            Payload = [0x01, 0x02, 0x03, 0x04, 0x05]
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(1, parsed.Version);
        Assert.IsTrue(parsed.Delete);
        Assert.IsTrue(parsed.Encrypted);
        Assert.IsTrue(parsed.Compressed);
        Assert.AreEqual(0xFFFF, parsed.MsgIdHash);
        Assert.AreEqual(IPAddress.Parse("10.0.0.1"), parsed.OriginatingSource);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, parsed.AuthenticationData);
        Assert.AreEqual("application/sdp", parsed.PayloadType);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, parsed.Payload);
    }

    /// <summary>
    /// Tests that IPv6 addresses are correctly parsed.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_WithIPv6_ParsesCorrectly() {
        byte[] payload = [0x01, 0x02];
        SAPPacket original = new SAPPacket {
            Version = 1,
            OriginatingSource = IPAddress.Parse("fe80::1"),
            PayloadType = "application/sdp",
            Payload = payload
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(1, parsed.Version);
        Assert.AreEqual(IPAddress.Parse("fe80::1"), parsed.OriginatingSource);
        Assert.AreEqual("application/sdp", parsed.PayloadType);
        CollectionAssert.AreEqual(payload, parsed.Payload);
    }

    /// <summary>
    /// Tests that ToBytes and Parse preserve all data in a round-trip.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Roundtrip_PreservesAllData() {
        SAPPacket original = new SAPPacket {
            Version = 1,
            Delete = false,
            Encrypted = false,
            Compressed = false,
            MsgIdHash = 0x1234,
            OriginatingSource = IPAddress.Parse("172.16.0.1"),
            AuthenticationData = [],
            PayloadType = "application/sdp",
            Payload = Encoding.UTF8.GetBytes("v=0\r\no=Cavern 12345 1 IN IP4 239.69.0.1\r\ns=Test\r\nc=IN IP4 239.69.0.1/32\r\nt=0 0\r\nm=audio 5004 RTP/AVP 97\r\na=rtpmap:97 L16/48000/2\r\na=ptime:1\r\na=ts-refclk:ptp=IEEE1588-2008:00-00-00-00-00-00-00-00:0\r\na=mediaclk:direct=0")
        };

        byte[] bytes = original.ToBytes();
        SAPPacket parsed = SAPPacket.Parse(bytes);
        Assert.AreEqual(original.Version, parsed.Version);
        Assert.AreEqual(original.Delete, parsed.Delete);
        Assert.AreEqual(original.Encrypted, parsed.Encrypted);
        Assert.AreEqual(original.Compressed, parsed.Compressed);
        Assert.AreEqual(original.MsgIdHash, parsed.MsgIdHash);
        Assert.AreEqual(original.OriginatingSource, parsed.OriginatingSource);
        CollectionAssert.AreEqual(original.AuthenticationData, parsed.AuthenticationData);
        Assert.AreEqual(original.PayloadType, parsed.PayloadType);
        CollectionAssert.AreEqual(original.Payload, parsed.Payload);
    }

    /// <summary>
    /// Tests that a large payload produces a packet of the expected size.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithLargePayload_ProducesCorrectSize() {
        byte[] largePayload = new byte[1024];
        for (int i = 0; i < largePayload.Length; i++) {
            largePayload[i] = (byte)(i % 256);
        }

        SAPPacket packet = new SAPPacket {
            OriginatingSource = IPAddress.Loopback,
            Payload = largePayload
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(8 + 16 + largePayload.Length, bytes.Length);
        // Verify payload is at the end
        byte[] actualPayload = new byte[largePayload.Length];
        Buffer.BlockCopy(bytes, bytes.Length - largePayload.Length, actualPayload, 0, largePayload.Length);
        CollectionAssert.AreEqual(largePayload, actualPayload);
    }

    /// <summary>
    /// Tests that an empty payload produces a packet of the expected size.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithEmptyPayload_ProducesCorrectSize() {
        SAPPacket packet = new SAPPacket {
            OriginatingSource = IPAddress.Loopback,
            Payload = []
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(24, bytes.Length); // 2 (flags + authLen) + 2 (msgIdHash) + 4 (IPv4) + 16 ("application/sdp\0") = 24
    }

    /// <summary>
    /// Tests that an IPv6 packet produces a larger header than IPv4.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBytes_WithIPv6_ProducesLargerHeader() {
        SAPPacket packet = new SAPPacket {
            OriginatingSource = IPAddress.Parse("::1"),
            Payload = []
        };

        byte[] bytes = packet.ToBytes();
        Assert.AreEqual(36, bytes.Length); // 2 (flags + authLen) + 2 (msgIdHash) + 16 (IPv6) + 16 ("application/sdp\0") = 36
    }
}
