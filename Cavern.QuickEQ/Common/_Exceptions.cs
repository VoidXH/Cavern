using System;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Tells that the channel count of the inputs does not match.
    /// </summary>
    public class ChannelCountMismatchException : Exception {
        const string message = "Channel count of the inputs does not match.";

        /// <summary>
        /// Tells that the channel count of the inputs does not match.
        /// </summary>
        public ChannelCountMismatchException() : base(message) { }
    }

    /// <summary>
    /// Tells that an EQ Curve can't be created by a generic switch.
    /// </summary>
    public class NonGeneralizedCurveException : Exception {
        const string message = "This EQ should be created with its constructor.";

        /// <summary>
        /// Tells that an EQ Curve can't be created by a generic switch.
        /// </summary>
        public NonGeneralizedCurveException() : base(message) { }
    }
}