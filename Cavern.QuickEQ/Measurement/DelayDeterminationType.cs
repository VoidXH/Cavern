namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// What technique to use for calculating delay when working with phase curves.
    /// </summary>
    public enum DelayDeterminationType {
        /// <summary>
        /// Don't determine delay.
        /// </summary>
        None,
        /// <summary>
        /// The delay is the slope of a phase curve, detect the rise of that slope.
        /// </summary>
        Slope,
        /// <summary>
        /// Determine the peak sample's index in the impulse response as delay.
        /// </summary>
        ImpulsePeak,
        /// <summary>
        /// Determine the peak sample's index in the phase shifted impulse response as delay.
        /// </summary>
        HilbertPeak,
    }
}
