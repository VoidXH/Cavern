using System;

namespace Cavern.Channels {
    /// <summary>
    /// Tells that this operation requires a different sample rate than what's given.
    /// </summary>
    public class SampleRateMismatchException : Exception {
        const string message = "This operation requires a different sample rate than what's given.";

        /// <summary>
        /// Tells that this operation requires a different sample rate than what's given.
        /// </summary>
        public SampleRateMismatchException() : base(message) { }
    }
}
