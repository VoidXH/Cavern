using System;
using System.Runtime.CompilerServices;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Read custom length words from a bitstream.
    /// </summary>
    sealed class BitExtractor {
        /// <summary>
        /// Next bit to read.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Next bit to read from the back.
        /// </summary>
        public int BackPosition { get; private set; }

        /// <summary>
        /// New bits can be extracted.
        /// </summary>
        public bool Readable => source != null;

        /// <summary>
        /// Bytestream to get the data from.
        /// </summary>
        byte[] source;

        /// <summary>
        /// Construct an empty extractor to be expanded.
        /// </summary>
        public BitExtractor() => source = new byte[0];

        /// <summary>
        /// Construct an extractor to a bitstream.
        /// </summary>
        public BitExtractor(byte[] source) {
            this.source = source;
            if (source != null) {
                BackPosition = source.Length * 8;
            }
        }

        /// <summary>
        /// Construct an extractor to a bitstream with a truncated length of the <paramref name="source"/>.
        /// </summary>
        public BitExtractor(byte[] source, int lastByte) {
            this.source = source;
            BackPosition = lastByte * 8;
        }

        /// <summary>
        /// Make sure the next expansion will replace all data.
        /// </summary>
        public void Clear() => Position = BackPosition;

        /// <summary>
        /// Add more bytes to the read queue.
        /// </summary>
        public void Expand(byte[] source) {
            int skip = Position >> 3,
                split = this.source.Length - skip;
            byte[] newSource = new byte[split + source.Length];
            Array.Copy(this.source, skip, newSource, 0, split);
            Array.Copy(source, 0, newSource, split, source.Length);
            this.source = newSource;
            Position -= skip * 8;
            BackPosition = this.source.Length * 8;
        }

        /// <summary>
        /// Read the next custom length unsigned word.
        /// </summary>
        public int Read(int bits) {
            int result = 0;
            while (bits > 0) {
                int removedLeft = Position & 7;
                int removedRight = 0;
                if (removedLeft + bits < 8) {
                    removedRight = 8 - removedLeft - bits;
                }
                int shiftBack = removedLeft + removedRight;
                int readBits = 8 - shiftBack;
                result = (result << readBits) + (((source[Position >> 3] << removedLeft) & 0xFF) >> shiftBack);
                bits -= readBits;
                Position += readBits;
            }
            return result;
        }

        /// <summary>
        /// Read the next custom length unsigned word, if a flag is set before it.
        /// </summary>
        public int? ReadConditional(int bits) {
            if (((source[Position >> 3] >> (7 - (Position++ & 7))) & 1) != 0) {
                return Read(bits);
            }
            return null;
        }

        /// <summary>
        /// Read the next custom length signed word.
        /// </summary>
        public int ReadSigned(int bits) {
            int value = Read(bits);
            int sign = value & (1 << bits);
            return sign << (31 - bits) + value - sign;
        }

        /// <summary>
        /// Read the next single bit as a flag.
        /// </summary>
        public bool ReadBit() => ReadBitInt() == 1;

        /// <summary>
        /// Read the next bit and advance the position.
        /// </summary>
        public int ReadBitInt() => (source[Position >> 3] >> (7 - (Position++ & 7))) & 1;

        /// <summary>
        /// Read the next masked flag value as an array.
        /// </summary>
        public bool[] ReadBits(int bits) {
            bool[] result = new bool[bits];
            while (bits-- > 0) {
                result[bits] = ReadBit();
            }
            return result;
        }

        /// <summary>
        /// Append <paramref name="count"/> bits to the <paramref name="buffer"/>.
        /// </summary>
        public void ReadBitsInto(ref byte[] buffer, int count) {
            int fullBytes = count >> 3,
                remainder = count & 7,
                bytesNeeded = fullBytes + (remainder != 0 ? 1 : 0);
            if (bytesNeeded > buffer.Length) {
                Array.Resize(ref buffer, bytesNeeded);
            }
            for (int i = 0; i < fullBytes; ++i) {
                buffer[i] = (byte)Read(8);
            }
            if (remainder != 0) {
                buffer[fullBytes] = (byte)(Read(remainder) << (8 - remainder));
            }
        }

        /// <summary>
        /// Append <paramref name="count"/> bytes to the <paramref name="buffer"/>.
        /// </summary>
        public void ReadBytesInto(ref byte[] buffer, ref int offset, int count) {
            if (offset + count > buffer.Length) {
                Array.Resize(ref buffer, offset + count);
            }
            while (count-- > 0) {
                buffer[offset++] = (byte)Read(8);
            }
        }

        /// <summary>
        /// Skip some bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count) => Position += count;

        /// <summary>
        /// Get a byte at a fixed position of the input data.
        /// </summary>
        /// <remarks><see cref="Expand(byte[])"/> can remove bytes from the beginning of the cache.</remarks>
        public byte this[int index] => source[index];
    }
}