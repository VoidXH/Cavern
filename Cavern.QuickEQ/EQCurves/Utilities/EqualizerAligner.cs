using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;

namespace Cavern.QuickEQ.EQCurves.Utilities {
    /// <summary>
    /// Aligns the level of each channel to 0 dB average within its stable range, so that only the shape of the room response remains.
    /// </summary>
    public sealed class EqualizerAligner {
        /// <summary>
        /// Frequency range limits considered for alignment on the main and LFE channels.
        /// </summary>
        public FrequencyRangeLimit FrequencyLimits { get; set; } = new FrequencyRangeLimit(100, 1000, 40, 80);

        /// <summary>
        /// Offset each channel to 0 dB in the stable range (main or LFE depending on channel layout).
        /// </summary>
        /// <param name="source">Curves to align.</param>
        public Equalizer[] Align(Equalizer[] source) {
            for (int i = 0; i < source.Length; i++) {
                Equalizer channel = (Equalizer)source[i].Clone();
                (double min, double max) = FrequencyLimits.GetLimits(Channel.IsLFE(i, source.Length));
                channel.Offset(-channel.GetAverageLevel(10, min, max));
                source[i] = channel;
            }
            return source;
        }
    }
}
