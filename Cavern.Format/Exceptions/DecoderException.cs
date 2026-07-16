using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if the decoder ran into a predefined error code that is found in the decoder's documentation.
    /// </summary>
    public class DecoderException : Exception {
        const string message = "Internal decoder error code: {0}.";

        /// <summary>
        /// Tells if the decoder ran into a predefined error code that is found in the decoder's documentation.
        /// </summary>
        public DecoderException(int errorCode) : base(string.Format(message, errorCode)) { }
    }
}
