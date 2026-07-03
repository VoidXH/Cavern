using System;
using System.Buffers.Binary;

using Cavern.Format.Networking.Exceptions;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Represents an RTP (Real-time Transport Protocol) packet with fields for version, payload type, sequence number, timestamp, SSRC, and payload data.
    /// </summary>
    public class RTPPacket {
        /// <summary>
        /// Gets or sets the RTP version number. Must be 2 for RTP version 2.
        /// </summary>
        public byte Version { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether the packet contains padding bytes after the payload.
        /// </summary>
        public bool Padding { get; set; }

        /// <summary>
        /// Gets or sets whether an extension header follows the RTP header.
        /// </summary>
        public bool Extension { get; set; }

        /// <summary>
        /// Gets or sets the number of CSRC identifiers in the header.
        /// </summary>
        public byte CSRCCount { get; set; }

        /// <summary>
        /// Gets or sets the marker bit, used by payload formats to mark significant events.
        /// </summary>
        public bool Marker { get; set; }

        /// <summary>
        /// Gets or sets the payload type identifier (7 bits).
        /// </summary>
        public byte PayloadType { get; set; }

        /// <summary>
        /// Gets or sets the sequence number, incremented for each RTP packet sent.
        /// </summary>
        public ushort SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the timestamp, reflecting the sampling instant of the first byte.
        /// </summary>
        public uint Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the synchronization source identifier (SSRC).
        /// </summary>
        public uint SSRC { get; set; }

        /// <summary>
        /// Gets or sets the payload data of the RTP packet.
        /// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Parses a raw byte array into an <see cref="RTPPacket"/>.
        /// </summary>
        /// <param name="data">The raw RTP packet bytes.</param>
        /// <returns>A parsed <see cref="RTPPacket"/> instance.</returns>
        public static RTPPacket Parse(ReadOnlySpan<byte> data) {
            if (data.Length < HeaderSize) {
                throw new InvalidPacketException("Packet too small to be RTP.");
            }

            RTPPacket packet = new RTPPacket {
                Version = (byte)(data[0] >> 6),
                Padding = ((data[0] >> 5) & 1) == 1,
                Extension = ((data[0] >> 4) & 1) == 1,
                CSRCCount = (byte)(data[0] & 0x0F),
                Marker = ((data[1] >> 7) & 1) == 1,
                PayloadType = (byte)(data[1] & 0x7F),
                SequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2)),
                Timestamp = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4)),
                SSRC = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(8, 4))
            };

            int payloadOffset = HeaderSize + packet.CSRCCount * 4;
            if (packet.Extension) {
                if (data.Length < payloadOffset + 4) {
                    throw new InvalidPacketException("Packet too small for extension header.");
                }
                int extLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(payloadOffset + 2, 2)) * 4;
                payloadOffset += 4 + extLength;
            }

            if (payloadOffset < data.Length) {
                packet.Payload = data[payloadOffset..].ToArray();
            }

            return packet;
        }

        /// <summary>
        /// Serializes this RTP packet into a raw byte array suitable for network transmission.
        /// </summary>
        /// <returns>A byte array containing the RTP header and payload.</returns>
        public byte[] ToBytes() {
            byte[] packet = new byte[HeaderSize + Payload.Length];
            packet[0] = (byte)((Version << 6) | (Padding ? 1 << 5 : 0) | (Extension ? 1 << 4 : 0) | CSRCCount);
            packet[1] = (byte)((Marker ? 1 << 7 : 0) | (PayloadType & 0x7F));
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), SequenceNumber);
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4, 4), Timestamp);
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(8, 4), SSRC);

            if (Payload.Length > 0) {
                Buffer.BlockCopy(Payload, 0, packet, HeaderSize, Payload.Length);
            }

            return packet;
        }

        /// <summary>
        /// The size of the RTP header in bytes (12 bytes minimum).
        /// </summary>
        const int HeaderSize = 12;
    }
}
