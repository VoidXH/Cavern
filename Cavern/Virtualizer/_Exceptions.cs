using System;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Tells if a ground channel is present, preventing an opteration.
    /// </summary>
    public class NonGroundChannelPresentException : Exception {
        const string message = "A non-ground channel present in the layout is preventing this operation.";

        /// <summary>
        /// Tells if a ground channel is present and preventing an opteration.
        /// </summary>
        public NonGroundChannelPresentException() : base(message) { }

        /// <summary>
        /// Tells if a ground channel is present and preventing an opteration, with a custom message.
        /// </summary>
        public NonGroundChannelPresentException(string message) : base(message) { }
    }
}