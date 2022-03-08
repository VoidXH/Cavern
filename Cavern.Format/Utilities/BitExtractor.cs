using Cavern.Utilities;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Read custom length words from a bitstream.
    /// </summary>
    internal sealed class BitExtractor {
        /// <summary>
        /// Bytestream to get the data from.
        /// </summary>
        byte[] source;

        /// <summary>
        /// Next bit to read.
        /// </summary>
        int position;

        /// <summary>
        /// Construct an extractor to a bitstream.
        /// </summary>
        public BitExtractor(byte[] source) {
            for (int i = 0; i < source.Length; ++i)
                source[i] = source[i].Revert();
            this.source = source;
        }

        /// <summary>
        /// Add more bytes to the read queue.
        /// </summary>
        public void Expand(byte[] source) {
            int skip = position >> 3,
                split = this.source.Length - skip;
            byte[] newSource = new byte[split + source.Length];
            for (int i = skip; i < this.source.Length; ++i)
                newSource[i - skip] = this.source[i];
            for (int i = 0; i < source.Length; ++i)
                newSource[i + split] = source[i].Revert();
            this.source = newSource;
            position -= skip * 8;
        }

        /// <summary>
        /// Gets the number of additional bytes required if <paramref name="forBits"/> has to be read.
        /// </summary>
        public int NeededBytes(int forBits) {
            int partial = position + forBits - source.Length * 8;
            return (partial >> 3) + (partial % 8 != 0 ? 1 : 0);
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
        /// Read the next single bit as a flag.
        /// </summary>
        public bool ReadBit() => NextBit() == 1;

        /// <summary>
        /// Skip some bits.
        /// </summary>
        public void Skip(int count) => position += count;

        /// <summary>
        /// Read the next bit and advance the position.
        /// </summary>
        int NextBit() {
            int result = (source[position / 8] >> (position % 8)) & 1;
            ++position;
            return result;
        }
    }
}