using System;

namespace Cavern.Remapping.Exceptions {
    /// <summary>
    /// Thrown when a &lt;= 7.1 standard layout is expected, but the input isn't one.
    /// </summary>
    public class LegacyInputExpectedException : Exception {
        const string message = "A legacy standard layout (<= 7.1) is expected, but the input is not one.";

        /// <summary>
        /// Thrown when a &lt;= 7.1 standard layout is expected, but the input isn't one.
        /// </summary>
        public LegacyInputExpectedException() : base(message) { }
    }
}
