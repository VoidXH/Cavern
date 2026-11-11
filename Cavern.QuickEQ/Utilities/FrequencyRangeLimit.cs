namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Frequency range limits for the main and LFE channels.
    /// </summary>
    public class FrequencyRangeLimit {
        /// <summary>
        /// Lowest frequency of the main channels in Hz.
        /// </summary>
        public double MinFreqMain { get; set; }

        /// <summary>
        /// Highest frequency of the main channels in Hz.
        /// </summary>
        public double MaxFreqMain { get; set; }

        /// <summary>
        /// Lowest frequency of the LFE channels in Hz.
        /// </summary>
        public double MinFreqLFE { get; set; }

        /// <summary>
        /// Highest frequency of the LFE channels in Hz.
        /// </summary>
        public double MaxFreqLFE { get; set; }

        /// <summary>
        /// Frequency range limits for the main and LFE channels.
        /// </summary>
        /// <param name="minFreqMain">Lowest frequency of the main channels in Hz.</param>
        /// <param name="maxFreqMain">Highest frequency of the main channels in Hz.</param>
        /// <param name="minFreqLFE">Lowest frequency of the LFE channels in Hz.</param>
        /// <param name="maxFreqLFE">Highest frequency of the LFE channels in Hz.</param>
        public FrequencyRangeLimit(double minFreqMain, double maxFreqMain, double minFreqLFE, double maxFreqLFE) {
            MinFreqMain = minFreqMain;
            MaxFreqMain = maxFreqMain;
            MinFreqLFE = minFreqLFE;
            MaxFreqLFE = maxFreqLFE;
        }

        /// <summary>
        /// Get the frequency range limits for the given channel type.
        /// </summary>
        /// <param name="lfe">Whether the channel is an LFE channel.</param>
        /// <returns>The (min, max) frequency limits in Hz.</returns>
        public (double min, double max) GetLimits(bool lfe) => lfe ? (MinFreqLFE, MaxFreqLFE) : (MinFreqMain, MaxFreqMain);
    }
}
