using System;
using System.IO;

namespace Cavern.Format.Common {
    /// <summary>
    /// Stream reading extension functions.
    /// </summary>
    public static class StreamExtensions {
        /// <summary>
        /// Read a big endian 16-bit signed integer from a stream.
        /// </summary>
        public static short ReadInt16BE(this BinaryReader reader) {
            if (BitConverter.IsLittleEndian) {
                ushort value = reader.ReadUInt16();
                return (short)((value >> 8) + ((value & 0x00FF) << 8));
            }
            return reader.ReadInt16();
        }
    }
}