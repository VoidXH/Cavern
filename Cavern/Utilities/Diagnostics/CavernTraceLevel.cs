namespace Cavern.Utilities.Diagnostics {
    /// <summary>
    /// Types of trace messages sent by <see cref="CavernTracing"/>.
    /// </summary>
    public enum CavernTraceLevel {
        /// <summary>
        /// Message about minor operating conditions.
        /// </summary>
        Debug,

        /// <summary>
        /// Message about ordinary events.
        /// </summary>
        Info,

        /// <summary>
        /// Message about conditions that might result in <see cref="Error"/>s later.
        /// </summary>
        Warning,

        /// <summary>
        /// Message about state-breaking events.
        /// </summary>
        Error
    }
}
