using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells that a required element is missing from the stream.
    /// </summary>
    public class MissingElementException : Exception {
        const string message = "A required element ({0}) is missing from the stream.";

        const string messageWithLocation = "A required element ({0}) was not found at 0x{1} in the stream.";

        /// <summary>
        /// Tells that a required element is missing from the stream.
        /// </summary>
        public MissingElementException(string element) : base(string.Format(message, element)) { }

        /// <summary>
        /// Tells that a required element is missing from the stream, and suggests a position for it.
        /// </summary>
        public MissingElementException(string element, long position) : base(string.Format(messageWithLocation, element, position.ToString("X"))) { }
    }
}
