using System;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when a channel was used before it was created.
    /// </summary>
    public class LastSplitPointException : Exception {
        const string message = "The last split point can't be removed, configuration files are made of at least one working unit (split point).";

        /// <summary>
        /// Thrown when a channel was used before it was created.
        /// </summary>
        public LastSplitPointException() : base(message) { }
    }
}
