namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// What technique to use for removing the effect of delay when working with phase curves.
    /// </summary>
    public enum PhaseDelayCompensationType {
        /// <summary>
        /// Do not try to remove delay.
        /// </summary>
        None,
        /// <summary>
        /// The delay is the slope of a phase curve, detect the slope and remove it.
        /// </summary>
        Slope,
        /// <summary>
        /// Remove as many samples of delay as far the impulse peak is away from the beginning of the signal.
        /// </summary>
        ImpulsePeak,
    }
}
