using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.EQCurves.Utilities {
    /// <summary>
    /// Aligns the level of each channel to 0 dB average within its stable range, so that only the shape of the room response remains.
    /// </summary>
    public sealed class EqualizerAligner {
        /// <summary>
        /// First frequency considered for alignment on main channels.
        /// </summary>
        public double MinMainFrequency { get; set; } = 100;

        /// <summary>
        /// Last frequency considered for alignment on main channels.
        /// </summary>
        public double MaxMainFrequency { get; set; } = 1000;

        /// <summary>
        /// First frequency considered for alignment on LFE channels.
        /// </summary>
        public double MinLFEFrequency { get; set; } = 40;

        /// <summary>
        /// Last frequency considered for alignment on LFE channels.
        /// </summary>
        public double MaxLFEFrequency { get; set; } = 80;

        /// <summary>
        /// Offset each channel to 0 dB in the stable range (main or LFE depending on channel layout).
        /// </summary>
        /// <param name="source">Curves to align.</param>
        public Equalizer[] Align(Equalizer[] source) {
            for (int i = 0; i < source.Length; i++) {
                Equalizer channel = (Equalizer)source[i].Clone();
                double average;
                if (!Channel.IsLFE(i, source.Length)) {
                    average = channel.GetAverageLevel(10, MinMainFrequency, MaxMainFrequency);
                } else {
                    average = channel.GetAverageLevel(10, MinLFEFrequency, MaxLFEFrequency);
                }
                channel.Offset(-average);
                source[i] = channel;
            }
            return source;
        }
    }
}
