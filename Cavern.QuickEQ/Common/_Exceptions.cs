using System;

namespace Cavern.QuickEQ {
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