using Cavern.Format.Common;

namespace Cavern.Format.Decoders {
    internal partial class EnhancedAC3Decoder {
        /// <summary>
        /// Decoder version check.
        /// </summary>
        static Decoder ParseDecoder(int bsid) {
            if (bsid <= (int)Decoder.AC3)
                return Decoder.AC3;
            else if (bsid == (int)Decoder.EAC3)
                return Decoder.EAC3;
            else
                throw new UnsupportedFeatureException("decoder " + bsid);
        }
    }
}