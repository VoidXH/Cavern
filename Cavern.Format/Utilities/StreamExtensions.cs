using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static Cavern.Utilities.QMath;

using Stream = System.IO.Stream;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Stream reading extension functions. Provides functionality similar to <see cref="BinaryReader"/> with better performance.
    /// </summary>
    static class StreamExtensions {
        /// <summary>
        /// Read more than 2 GB into a buffer.
        /// </summary>
        public static void Read(this Stream reader, byte[] buffer, long start, long length) {
            long position = start;
            length += start;
            while (position != length) {
                int step = (int)Math.Min(length - position, int.MaxValue);
                int read = reader.Read(buffer, 0, step);
                if (read != step) {
                    return;
                }
                position += step;
            }
        }

        /// <summary>
        /// Read a fixed-length ASCII string from the stream.
        /// </summary>
        public static string ReadASCII(this Stream reader, int length) {
            char[] result = new char[length];
            for (int i = 0; i < length; i++) {
                result[i] = (char)reader.ReadByte();
            }
            return new string(result);
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
        /// Read a number of bytes from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytes(this Stream reader, uint length) {
            byte[] bytes = new byte[length];
            reader.Read(bytes);
            return bytes;
        }

        /// <summary>
        /// Reads an ASCII string with a closing 0.
        /// </summary>
        public static string ReadCString(this Stream reader) {
            List<byte> result = new List<byte>();
            while (true) {
                int read = reader.ReadByte();
                if (read > 0) {
                    result.Add((byte)read);
                } else {
                    break;
                }
            }
            return Encoding.ASCII.GetString(result.ToArray());
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
            return (short)(BitConverter.IsLittleEndian ? (a << 8) | b : ((b << 8) | a));
        }

        /// <summary>
        /// Read a 32-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this Stream reader) =>
            reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16) | (reader.ReadByte() << 24);

        /// <summary>
        /// Read a big-endian 32-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this Stream reader) =>
            BitConverter.IsLittleEndian ?
                (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte() :
                reader.ReadInt32();

        /// <summary>
        /// Read a 64-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this Stream reader) =>
            (long)reader.ReadByte() | ((long)reader.ReadByte() << 8) | ((long)reader.ReadByte() << 16) | ((long)reader.ReadByte() << 24) |
            ((long)reader.ReadByte() << 32) | ((long)reader.ReadByte() << 40) |
            ((long)reader.ReadByte() << 48) | ((long)reader.ReadByte() << 56);

        /// <summary>
        /// Read a 16-bit signed integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this Stream reader) => (ushort)(reader.ReadByte() | (reader.ReadByte() << 8));

        /// <summary>
        /// Read a big-endian 32-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this Stream reader) => (ushort)reader.ReadInt16BE();

        /// <summary>
        /// Read a 32-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this Stream reader) => (uint)reader.ReadInt32();

        /// <summary>
        /// Read a big-endian 32-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32BE(this Stream reader) => (uint)reader.ReadInt32BE();

#pragma warning disable CS0675 // False positive: Bitwise-or operator used on a sign-extended operand
        /// <summary>
        /// Read a 64-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this Stream reader) =>
            (ulong)reader.ReadByte() | ((ulong)reader.ReadByte() << 8) | ((ulong)reader.ReadByte() << 16) |
            ((ulong)reader.ReadByte() << 24) | ((ulong)reader.ReadByte() << 32) | ((ulong)reader.ReadByte() << 40) |
            ((ulong)reader.ReadByte() << 48) | ((ulong)reader.ReadByte() << 56);

        /// <summary>
        /// Read a big-endian 64-bit unsigned integer from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64BE(this Stream reader) =>
            ((ulong)reader.ReadByte() << 56) | ((ulong)reader.ReadByte() << 48) | ((ulong)reader.ReadByte() << 40) |
            ((ulong)reader.ReadByte() << 32) | ((ulong)reader.ReadByte() << 24) | ((ulong)reader.ReadByte() << 16) |
            ((ulong)reader.ReadByte() << 8) | (ulong)reader.ReadByte();
#pragma warning restore CS0675

        /// <summary>
        /// Read a 32-bit floating point number from the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingle(this Stream reader) => new ConverterStruct {
            asInt = reader.ReadInt32()
        }.asFloat;

        /// <summary>
        /// Tests if the next byte block is as expected, throws an exception if it's not.
        /// </summary>
        public static void BlockTest(this Stream reader, byte[] block) {
            byte[] input = reader.ReadBytes(block.Length);
            for (int i = 0; i < block.Length; ++i) {
                if (input[i] != block[i]) {
                    throw new IOException("Format mismatch.");
                }
            }
        }

        /// <summary>
        /// Tests if the next rolling byte block is as expected, if not, it advances by 1 byte.
        /// </summary>
        public static bool RollingBlockCheck(this Stream reader, byte[] cache, byte[] block) {
            for (int i = 1; i < cache.Length; ++i) {
                cache[i - 1] = cache[i];
            }
            cache[^1] = (byte)reader.ReadByte();
            for (int i = 0; i < block.Length; ++i) {
                if (cache[i] != block[i]) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Write any value to the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAny<T>(this Stream writer, T value) where T : struct =>
            writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)));
    }
}