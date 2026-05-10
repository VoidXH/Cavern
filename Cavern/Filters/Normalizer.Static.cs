using Cavern.Utilities;

namespace Cavern.Filters {
    // General audio normalizing features
    partial class Normalizer {
        /// <summary>
        /// Set the gain of a single channel's <paramref name="samples"/> to a target RMS <paramref name="level"/>.
        /// </summary>
        public static void NormalizeTo(float[] samples, int level) {
            float rms = samples.GetRMS();
            WaveformUtils.Gain(samples, level / rms);
        }

        /// <summary>
        /// Set the gain of a clip to a target RMS <paramref name="level"/>.
        /// </summary>
        public static void NormalizeTo(Clip clip, int level) {
            float rms = clip.Data.GetRMS();
            clip.Data.Gain(level / rms);
        }
    }
}
