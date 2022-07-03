using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cavern.Format.Common {
    /// <summary>
    /// Stream reading extension functions.
    /// </summary>
    public static class StreamExtensions {
        /// <summary>
        /// Read a number of bytes from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytes(this Stream reader, int length) {
            byte[] bytes = new byte[length];
            reader.Read(bytes);
            return bytes;
        }

        /// <summary>
        /// Read a big endian 16-bit signed integer from a stream.
        /// </summary>
        public static short ReadInt16BE(this Stream reader) {
            byte[] raw = new byte[2];
            reader.Read(raw);
            if (BitConverter.IsLittleEndian)
                (raw[0], raw[1]) = (raw[1], raw[0]);
            return BitConverter.ToInt16(raw);
        }
    }
}