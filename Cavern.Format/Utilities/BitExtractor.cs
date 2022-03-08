using Cavern.Utilities;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Read custom length words from a bitstream.
    /// </summary>
    internal sealed class BitExtractor {
        /// <summary>
        /// Bytestream to get the data from.
        /// </summary>
        readonly byte[] source;

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
        /// Read the next bit and advance the position.
        /// </summary>
        int NextBit() {
            int result = (source[position / 8] >> (position % 8)) & 1;
            ++position;
            return result;
        }
    }
}