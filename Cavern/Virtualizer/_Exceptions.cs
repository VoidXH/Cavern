using System;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Tells if a ground channel is present, preventing an opteration.
    /// </summary>
    public class NonGroundChannelPresentException : Exception {
        const string message = "The active layout does not support height virtualization on speakers. " +
            "Either disable the option or choose a layout with ground channels only.";

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
