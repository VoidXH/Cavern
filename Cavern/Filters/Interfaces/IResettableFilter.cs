namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// Allows a filter to clear its internal state for reuse.
    /// </summary>
    public interface IResettableFilter {
        /// <summary>
        /// Reset the filter's internal state for reuse. Prepares the filter for processing a new and different signal.
        /// </summary>
        void Reset();
    }
}
