namespace Cavern.Format.FilterSet.Enums {
    /// <summary>
    /// In what to measure delays when exporting a filter set.
    /// </summary>
    public enum DelayUnit {
        /// <summary>
        /// Measure in audio samples on the filter's sample rate.
        /// </summary>
        Samples,
        /// <summary>
        /// Measure in time: milliseconds.
        /// </summary>
        Milliseconds,
        /// <summary>
        /// Measure in distance: centimeters.
        /// </summary>
        Centimeters,
    }
}
