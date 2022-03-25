using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Extension methods used for EMDF decoding.
    /// </summary>
    static class ExtensibleMetadataExtensions {
        /// <summary>
        /// Read variable-length values from an EMDF stream.
        /// </summary>
        public static int VariableBits(this BitExtractor extractor, int bits) {
            int value = 0;
            bool readMore;
            do {
                value += extractor.Read(bits);
                if (readMore = extractor.ReadBit()) {
                    value <<= bits;
                    value += 1 << bits;
                }
            } while (readMore);
            return value;
        }
    }
}