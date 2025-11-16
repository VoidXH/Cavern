using System;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when a channel was used before it was created.
    /// </summary>
    public class NotPreparedChannelException : Exception {
        const string message = "A channel was used before it was created: {0}.";

        /// <summary>
        /// Thrown when a channel was used before it was created.
        /// </summary>
        public NotPreparedChannelException(string chName) : base(string.Format(message, chName)) { }
    }
}
