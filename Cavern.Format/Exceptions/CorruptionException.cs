using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if the decoded stream is corrupted.
    /// </summary>
    public class CorruptionException : Exception {
        const string message = "The decoder found corrupted data at {0}.";

        /// <summary>
        /// Tells if the decoded stream is corrupted.
        /// </summary>
        public CorruptionException(string location) : base(string.Format(message, location)) { }
    }
}
