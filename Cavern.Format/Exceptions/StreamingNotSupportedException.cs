using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a codec can't be streamed.
    /// </summary>
    public class StreamingNotSupportedException : Exception {
        const string message = "This codec can't be streamed.";

        /// <summary>
        /// Tells if a codec can't be streamed.
        /// </summary>
        public StreamingNotSupportedException() : base(message) { }
    }
}
