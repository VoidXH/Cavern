using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells that no program was detected in the container/track.
    /// </summary>
    public class NoProgramException : Exception {
        const string message = "No program was detected.";

        /// <summary>
        /// Tells that no program was detected in the container/track.
        /// </summary>
        public NoProgramException() : base(message) { }
    }
}
