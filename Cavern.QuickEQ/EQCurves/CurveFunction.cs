namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Available built-in target curves.
    /// </summary>
    public enum CurveFunction {
        /// <summary>
        /// Uniform gain on all frequencies.
        /// </summary>
        Flat,
        /// <summary>
        /// Cinema standard curve.
        /// </summary>
        XCurve,
        /// <summary>
        /// Adds a bass bump for punch emphasis.
        /// </summary>
        Punch,
        /// <summary>
        /// Adds a sub-bass slope for depth emphasis.
        /// </summary>
        Depth,
        /// <summary>
        /// Bandpass EQ curve, recommended for stage subwoofers.
        /// </summary>
        Bandpass,
        /// <summary>
        /// Frequently used target curve for very small rooms.
        /// </summary>
        RoomCurve,
        /// <summary>
        /// An EQ curve with any amount of custom bands.
        /// </summary>
        Custom,
        /// <summary>
        /// Smooths out inter-channel differences while keeping the system's sound character.
        /// </summary>
        Smoother
    }
}