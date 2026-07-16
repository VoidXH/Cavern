using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells that a decoder which can process an infinite stream is not able to return content length.
    /// </summary>
    public class RealtimeLengthException : Exception {
        const string message = "This is an infinite decoder. Content lenth is not readable from the bitstream.";

        /// <summary>
        /// Tells that a decoder which can process an infinite stream is not able to return content length.
        /// </summary>
        public RealtimeLengthException() : base(message) { }
    }
}
