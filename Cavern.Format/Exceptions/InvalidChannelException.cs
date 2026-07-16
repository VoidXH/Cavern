using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if some channels are not supported by the operation.
    /// </summary>
    public class InvalidChannelException : Exception {
        const string messageForOperation = "A channel ({0}) is not supported by the operation.";

        /// <summary>
        /// Tells if a channel is not supported by the operation.
        /// </summary>
        public InvalidChannelException(string name) : base(string.Format(messageForOperation, name)) { }
    }
}
