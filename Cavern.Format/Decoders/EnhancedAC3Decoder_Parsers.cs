using Cavern.Format.Common;
using Cavern.Utilities;

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
                throw new UnsupportedFeatureException("decoder version " + bsid);
        }

        /// <summary>
        /// The <paramref name="dialnorm"/> variable tells how far the average dialog level is below 0 dB FS.
        /// </summary>
        static float ParseDialogNormalization(int dialnorm) => QMath.DbToGain(dialnorm != 0 ? -dialnorm : -31);
    }
}