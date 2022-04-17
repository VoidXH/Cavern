using System;

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
        public int BackPosition { get; set; }

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
            BackPosition = source.Length * 8;
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
        /// Check the next bits without advancing the position.
        /// </summary>
        public int Peek(int bits) {
            int result = Read(bits);
            Position -= bits;
            return result;
        }

        /// <summary>
        /// Read the next custom length unsigned word.
        /// </summary>
        public int Read(int bits) {
            int result = 0;
            while (bits > 0) {
                int removedLeft = Position & 7;
                int removedRight = 0;
                if (removedLeft + bits < 8)
                    removedRight = 8 - removedLeft - bits;
                int shiftBack = removedLeft + removedRight;
                int readBits = 8 - shiftBack;
                result = (result << readBits) + (((source[Position / 8] << removedLeft) & 0xFF) >> shiftBack);
                bits -= readBits;
                Position += readBits;
            }
            return result;
        }

        /// <summary>
        /// Read the next custom length unsigned word, if a flag is set before it.
        /// </summary>
        public int? ReadConditional(int bits) {
            if (((source[Position / 8] >> (7 - Position++ % 8)) & 1) != 0)
                return Read(bits);
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
        public bool ReadBit() => NextBit() == 1;

        /// <summary>
        /// Read the next masked flag value as an array.
        /// </summary>
        public bool[] ReadBits(int bits) {
            bool[] result = new bool[bits];
            while (bits-- > 0)
                result[bits] = ReadBit();
            return result;
        }

        /// <summary>
        /// Read a byte array, even if it's offset from byte borders.
        /// </summary>
        public byte[] ReadBytes(int count) {
            byte[] result = new byte[count];
            for (int i = 0; i < count; ++i)
                result[i] = (byte)Read(8);
            return result;
        }

        /// <summary>
        /// Skip some bits.
        /// </summary>
        public void Skip(int count) => Position += count;

        /// <summary>
        /// Get a byte at a fixed position of the input data.
        /// </summary>
        /// <remarks><see cref="Expand(byte[])"/> can remove bytes from the beginning of the cache.</remarks>
        public byte this[int index] => source[index];

        /// <summary>
        /// Read the next bit and advance the position.
        /// </summary>
        int NextBit() => (source[Position / 8] >> (7 - Position++ % 8)) & 1;
    }
}