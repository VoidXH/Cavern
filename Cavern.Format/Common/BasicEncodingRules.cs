using System.IO;

using Cavern.Format.Utilities;

namespace Cavern.Format.Common {
    /// <summary>
    /// Handles values encoded in Basic Encoding Rules (BER) format.
    /// </summary>
    public static class BasicEncodingRules {
        /// <summary>
        /// Read a BER value from a stream.
        /// </summary>
        public static long Read(Stream reader) {
            int bytes = reader.ReadByte();
            if ((bytes & 0x80) == 0) {
                return bytes;
            }
            return bytes switch {
                0x81 => reader.ReadByte(),
                0x82 => reader.ReadUInt16BE(),
                0x83 => (reader.ReadUInt16BE() << 8) | reader.ReadByte(),
                0x84 => reader.ReadUInt32BE(),
                _ => throw new UnsupportedFeatureException("long BER")
            };
        }
    }
}