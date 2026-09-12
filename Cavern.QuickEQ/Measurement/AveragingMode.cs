namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// How measurements across multiple positions are averaged.
    /// </summary>
    public enum AveragingMode {
        /// <summary>
        /// Average in the frequency domain (average linear gains).
        /// </summary>
        FrequencyDomain,

        /// <summary>
        /// Average in the frequency domain using RMS (average squared magnitudes, then take square root).
        /// </summary>
        FrequencyDomainRMS,
    }
}
