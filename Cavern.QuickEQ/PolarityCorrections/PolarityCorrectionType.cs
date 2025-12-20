namespace Cavern.QuickEQ.PolarityCorrections {
    /// <summary>
    /// Supported methods to detect and correct swapped polarities.
    /// </summary>
    public enum PolarityCorrectionType {
        /// <summary>
        /// Polarity correction is disabled.
        /// </summary>
        None,
        /// <summary>
        /// Checks if pairs of channels add together constructively or destructively.
        /// </summary>
        ConstructivityBased,
        /// <summary>
        /// Aligns impulse peaks to the same direction.
        /// </summary>
        ImpulsePeakBased,
    }
}
