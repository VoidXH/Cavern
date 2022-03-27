using Cavern.Utilities;

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
        /// Construct an extractor to a bitstream.
        /// </summary>
        public BitExtractor(byte[] source, bool revert = true) {
            if (revert)
                for (int i = 0; i < source.Length; ++i)
                    source[i] = source[i].Revert();
            this.source = source;
            BackPosition = source.Length * 8;
        }

        /// <summary>
        /// Add more bytes to the read queue.
        /// </summary>
        public void Expand(byte[] source) {
            int skip = Position >> 3,
                split = this.source.Length - skip;
            byte[] newSource = new byte[split + source.Length];
            for (int i = skip; i < this.source.Length; ++i)
                newSource[i - skip] = this.source[i];
            for (int i = 0; i < source.Length; ++i)
                newSource[i + split] = source[i].Revert();
            this.source = newSource;
            Position -= skip * 8;
            BackPosition = this.source.Length * 8;
        }

        /// <summary>
        /// Gets the number of additional bytes required if <paramref name="forBits"/> has to be read.
        /// </summary>
        public int NeededBytes(int forBits) {
            int partial = Position + forBits - source.Length * 8;
            return (partial >> 3) + (partial % 8 != 0 ? 1 : 0);
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
        /// Read the next custom length word.
        /// </summary>
        public int Read(int bits) {
            int result = 0;
            while (--bits >= 0)
                result = (result << 1) | NextBit();
            return result;
        }

        /// <summary>
        /// Read the next custom length word from the back.
        /// </summary>
        public int ReadBack(int bits) {
            int oldPos = Position;
            Position = BackPosition -= bits;
            int result = Read(bits);
            Position = oldPos;
            return result;
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
        /// Read the next bit and advance the position.
        /// </summary>
        int NextBit() {
            int result = (source[Position / 8] >> (Position % 8)) & 1;
            ++Position;
            return result;
        }
    }
}