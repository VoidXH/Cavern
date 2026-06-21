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
        /// A version of slope-detection where the impulse-response is windowed to the +/-64 sample area of the <see cref="ImpulseEnvelopePeak"/>.
        /// This is practically the same result as <see cref="ImpulseEnvelopePeak"/>, but with subsample precision.
        /// </summary>
        SlopeWindowed,
        /// <summary>
        /// Determine the peak sample's index in the impulse response as delay.
        /// </summary>
        ImpulsePeak,
        /// <summary>
        /// Determine the peak sample's index by the envelope of the impulse response.
        /// </summary>
        ImpulseEnvelopePeak,
    }
}
