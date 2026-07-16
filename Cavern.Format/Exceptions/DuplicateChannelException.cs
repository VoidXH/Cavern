using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a single channel is present multiple times in a stream that doesn't support it.
    /// </summary>
    public class DuplicateChannelException : Exception {
        const string message = "A single channel is present multiple times in a stream that doesn't support it.";

        /// <summary>
        /// Tells if a single channel is present multiple times in a stream that doesn't support it.
        /// </summary>
        public DuplicateChannelException() : base(message) { }
    }
}
