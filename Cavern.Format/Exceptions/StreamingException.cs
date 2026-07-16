using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells is a feature is only available when reading from a raw file.
    /// </summary>
    public class StreamingException : Exception {
        const string message = "This stream is read from a container or other wrapper. The operation you tried to perform should be done on the parent.";

        /// <summary>
        /// Tells is a feature is only available when reading from a raw file.
        /// </summary>
        public StreamingException() : base(message) { }
    }
}
