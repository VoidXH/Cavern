using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if the channel order cannot be applied as it's invalid in an export format.
    /// </summary>
    public class InvalidChannelOrderException : Exception {
        const string message = "The channel order cannot be applied as it's invalid in this export format.";

        /// <summary>
        /// Tells if the channel order cannot be applied as it's invalid in an export format.
        /// </summary>
        public InvalidChannelOrderException() : base(message) { }
    }
}
