using System;

namespace Cavern.Channels {
    /// <summary>
    /// Tells that this operation requires a different channel count than what's given.
    /// </summary>
    public class ChannelCountMismatchException : Exception {
        const string message = "This operation requires a different channel count than what's given.";

        /// <summary>
        /// Tells that this operation requires a different channel count than what's given.
        /// </summary>
        public ChannelCountMismatchException() : base(message) { }
    }
}
