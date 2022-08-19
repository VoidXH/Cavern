using System;
using System.IO;
using System.Runtime.CompilerServices;

using static Cavern.Utilities.QMath;

namespace Cavern.Format.Common {
    /// <summary>
    /// Stream reading extension functions. Provides functionality similar to <see cref="BinaryReader"/> with better performance.
    /// </summary>
    public static class StreamExtensions {
        /// <summary>
        /// Read more than 2 GB into a buffer.
        /// </summary>
        public static void Read(this Stream reader, byte[] buffer, long start, long length) {
            long position = start;
            length += start;
            while (position != length) {
                int step = (int)Math.Min(length - position, int.MaxValue);
                reader.Read(buffer, 0, step);
                position += step;
            }
        }

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
        /// Read a 16-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this Stream reader) => (short)(reader.ReadByte() | (reader.ReadByte() << 8));

        /// <summary>
        /// Read a big endian 16-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16BE(this Stream reader) {
            int a = reader.ReadByte(), b = reader.ReadByte();
            return (short)(BitConverter.IsLittleEndian ? (a << 8) + b : ((b << 8) + a));
        }

        /// <summary>
        /// Read a 32-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this Stream reader) =>
            reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16) | (reader.ReadByte() << 24);

        /// <summary>
        /// Read a 32-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this Stream reader) => BitConverter.ToInt64(reader.ReadBytes(8));

        /// <summary>
        /// Read a 32-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this Stream reader) => (uint)reader.ReadInt32();

        /// <summary>
        /// Read a 32-bit floating point number from the stream.
        /// </summary>
        public static float ReadSingle(this Stream reader) => new ConverterStruct() {
            asInt = reader.ReadInt32()
        }.asFloat;
    }
}