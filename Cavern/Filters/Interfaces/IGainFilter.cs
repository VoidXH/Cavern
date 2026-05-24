namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// A filter implementing gain control.
    /// </summary>
    public interface IGainFilter : IMultiplatformFilter {
        /// <summary>
        /// Filter gain in decibels.
        /// </summary>
        double GainValue { get; set; }

        /// <summary>
        /// Invert the phase in addition to changing gain.
        /// </summary>
        bool Invert { get; set; }
    }
}
