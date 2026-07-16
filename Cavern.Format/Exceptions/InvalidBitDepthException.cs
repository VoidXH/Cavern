using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if the selected bit depth is not supported for the current operation.
    /// </summary>
    public class InvalidBitDepthException : Exception {
        const string message = "The selected bit depth ({0}) is not supported.";

        /// <summary>
        /// Tells if the selected bit depth is not supported for the current operation.
        /// </summary>
        public InvalidBitDepthException(BitDepth bits) : base(string.Format(message, bits)) { }
    }
}
