using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Extension methods used for EMDF decoding.
    /// </summary>
    static class ExtensibleMetadataExtensions {
        /// <summary>
        /// Read variable-length values from an EMDF stream.
        /// </summary>
        public static int VariableBits(this BitExtractor extractor, byte bits) {
            int value = 0;
            bool readMore;
            do {
                value += extractor.Read(bits);
                if (readMore = extractor.ReadBit()) {
                    value = (value + 1) << bits;
                }
            } while (readMore);
            return value;
        }

        /// <summary>
        /// Read variable-length values from an EMDF stream with a limit on length.
        /// The <paramref name="limit"/> parameter takes 1 less than the actual length because reasons.
        /// </summary>
        public static int VariableBits(this BitExtractor extractor, byte bits, int limit) {
            int value = 0;
            bool readMore;
            do {
                value += extractor.Read(bits);
                if (readMore = extractor.ReadBit()) {
                    value = (value + 1) << bits;
                }
            } while (readMore && limit-- != 0);
            return value;
        }
    }
}