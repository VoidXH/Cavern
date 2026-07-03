using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Cavern.Format.Networking.Exceptions;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Represents a SAP (Session Announcement Protocol) packet (RFC 2974)
    /// containing version, message type flags, message ID hash, originating source, MIME type, and payload data.
    /// </summary>
    public class SAPPacket {
        /// <summary>
        /// Gets or sets the SAP version.
        /// </summary>
        public byte Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the message type. false = session announcement, true = session deletion.
        /// </summary>
        public bool Delete { get; set; }

        /// <summary>
        /// Gets or sets whether the payload is encrypted.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets whether the payload is compressed.
        /// </summary>
        public bool Compressed { get; set; }

        /// <summary>
        /// Gets or sets the message identifier hash.
        /// </summary>
        public ushort MsgIdHash { get; set; } = 0x0420;

        /// <summary>
        /// Gets or sets the originating source IP address.
        /// </summary>
        public IPAddress OriginatingSource { get; set; } = IPAddress.Loopback;

        /// <summary>
        /// Gets or sets the optional authentication data. Length must be a multiple of 4 bytes.
        /// </summary>
        public byte[] AuthenticationData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the MIME payload type string (e.g., "application/sdp").
        /// </summary>
        public string PayloadType { get; set; } = "application/sdp";

        /// <summary>
        /// Gets or sets the raw payload data.
        /// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Parses a raw byte array into a <see cref="SAPPacket"/>.
        /// </summary>
        /// <param name="data">The raw SAP packet bytes.</param>
        /// <returns>A parsed <see cref="SAPPacket"/> instance.</returns>
        public static SAPPacket Parse(ReadOnlySpan<byte> data) {
            if (data.Length < 8) {
                throw new InvalidPacketException("Packet too small to be SAP.");
            }

            byte flags = data[0];
            byte version = (byte)((flags >> 5) & 0x07);
            bool isIPv6 = (flags & 0x10) != 0;
            bool delete = (flags & 0x04) != 0;
            bool encrypted = (flags & 0x02) != 0;
            bool compressed = (flags & 0x01) != 0;

            int authLen = data[1] * 4;
            ushort msgIdHash = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2));

            int ipAddrLen = isIPv6 ? 16 : 4;
            int headerLen = 4 + ipAddrLen + authLen;

            if (data.Length < headerLen) {
                throw new InvalidPacketException("Packet too small for SAP header.");
            }

            IPAddress originatingSource;
            if (isIPv6) {
                originatingSource = new IPAddress(data[4..20].ToArray());
            } else {
                originatingSource = new IPAddress(data[4..8].ToArray());
            }

            byte[] authData = Array.Empty<byte>();
            if (authLen > 0) {
                authData = data.Slice(4 + ipAddrLen, authLen).ToArray();
            }

            // Find MIME null terminator starting at headerLen
            int mimeEnd = -1;
            for (int i = headerLen; i < data.Length; i++) {
                if (data[i] == 0) {
                    mimeEnd = i;
                    break;
                }
            }

            if (mimeEnd == -1) {
                throw new InvalidPacketException("MIME type null terminator not found.");
            }

            string payloadType = Encoding.ASCII.GetString(data[headerLen..mimeEnd].ToArray());
            int payloadStart = mimeEnd + 1;
            byte[] payload = Array.Empty<byte>();
            if (payloadStart < data.Length) {
                payload = data[payloadStart..].ToArray();
            }

            return new SAPPacket {
                Version = version,
                Delete = delete,
                Encrypted = encrypted,
                Compressed = compressed,
                MsgIdHash = msgIdHash,
                OriginatingSource = originatingSource,
                AuthenticationData = authData,
                PayloadType = payloadType,
                Payload = payload
            };
        }

        /// <summary>
        /// Serializes this SAP packet into a raw byte array suitable for network transmission.
        /// </summary>
        /// <returns>A byte array containing the SAP header, authentication data, payload type, and payload.</returns>
        public byte[] ToBytes() {
            bool isIPv6 = OriginatingSource.AddressFamily == AddressFamily.InterNetworkV6;
            int ipAddrLen = isIPv6 ? 16 : 4;
            int authLen = (AuthenticationData.Length + 3) / 4; // Round up to 32-bit words
            int paddedAuthLen = authLen * 4;

            byte[] mimeBytes = Encoding.ASCII.GetBytes(PayloadType + "\0");
            int totalHeaderSize = 4 + ipAddrLen + paddedAuthLen + mimeBytes.Length;
            byte[] packet = new byte[totalHeaderSize + Payload.Length];

            // Build byte 0: V (3 bits) | A (1 bit) | R (1 bit) | T (1 bit) | E (1 bit) | C (1 bit)
            byte flags = (byte)((Version & 0x07) << 5);
            if (isIPv6) {
                flags |= 0x10;
            }
            if (Delete) {
                flags |= 0x04;
            }
            if (Encrypted) {
                flags |= 0x02;
            }
            if (Compressed) {
                flags |= 0x01;
            }

            packet[0] = flags;
            packet[1] = (byte)authLen;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), MsgIdHash);
            byte[] ipBytes = OriginatingSource.GetAddressBytes();
            Buffer.BlockCopy(ipBytes, 0, packet, 4, ipAddrLen);
            if (AuthenticationData.Length > 0) {
                Buffer.BlockCopy(AuthenticationData, 0, packet, 4 + ipAddrLen, AuthenticationData.Length);
            }
            Buffer.BlockCopy(mimeBytes, 0, packet, 4 + ipAddrLen + paddedAuthLen, mimeBytes.Length);
            if (Payload.Length > 0) {
                Buffer.BlockCopy(Payload, 0, packet, totalHeaderSize, Payload.Length);
            }
            return packet;
        }
    }
}
