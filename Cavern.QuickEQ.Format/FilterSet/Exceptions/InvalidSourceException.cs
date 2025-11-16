using System;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Represents that this filter set was incorrectly initialized as a nonexistent or invalid file was read.
    /// </summary>
    public class InvalidSourceException : Exception {
        const string message = "This filter set was incorrectly initialized as a nonexistent or invalid file was read.";

        /// <summary>
        /// The target filter set couldn't be used for configuration without before/after measurements (target curve hacking),
        /// so regular filter set exports are not supported.
        /// </summary>
        public InvalidSourceException() : base(message) { }
    }
}
