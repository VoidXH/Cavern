namespace Cavern.QuickEQ.Equalization.Enums {
    /// <summary>
    /// Specifies the space in which smoothing is applied.
    /// </summary>
    public enum SmoothingMode {
        /// <summary>
        /// Smoothing is applied in decibel space (raw gain values).
        /// </summary>
        DecibelSpace,

        /// <summary>
        /// Smoothing is applied in linear gain space (converted from decibels).
        /// </summary>
        GainSpace,

        /// <summary>
        /// Smoothing is applied in complex space, averaging magnitude and phase independently to preserve amplitude.
        /// </summary>
        ComplexSpace,
    }
}
