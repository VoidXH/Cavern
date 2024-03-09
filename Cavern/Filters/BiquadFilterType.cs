namespace Cavern.Filters {
    /// <summary>
    /// Supported variants of biquad filters.
    /// </summary>
    public enum BiquadFilterType {
        /// <summary>
        /// Only affects the phase of the signal, selectively delay either side of the center frequency.
        /// </summary>
        Allpass,
        /// <summary>
        /// Filters the signal to a single band.
        /// </summary>
        Bandpass,
        /// <summary>
        /// Filters the signal to the high frequencies.
        /// </summary>
        Highpass,
        /// <summary>
        /// Elevates of lowers high frequency components of the signal.
        /// </summary>
        HighShelf,
        /// <summary>
        /// Filters the signal to the low frequencies.
        /// </summary>
        Lowpass,
        /// <summary>
        /// Elevates of lowers low frequency components of the signal.
        /// </summary>
        LowShelf,
        /// <summary>
        /// Removes a single band from the signal.
        /// </summary>
        Notch,
        /// <summary>
        /// Modifies the spectrum in a bell shape for a single band.
        /// </summary>
        PeakingEQ,
    }
}