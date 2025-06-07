namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Possible drawings to produce from various measurements.
    /// </summary>
    public enum DrawableMeasurementType {
        /// <summary>
        /// Any kind of curve: spectrum, phase, etc., using the <see cref="GraphRenderer"/>.
        /// </summary>
        Graph,
        /// <summary>
        /// Frequency intensity by time using the <see cref="STFTRenderer"/>.
        /// </summary>
        Spectogram,
    }
}
