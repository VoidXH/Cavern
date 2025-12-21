using System;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Thrown when a FIR channel's gain was tried to be accessed. There is no such thing, it's included in the filter coefficients.
    /// </summary>
    public class FIRGainException : Exception {
        const string message = "FIR filters have no gain values, it's included in the filter coefficients.";

        /// <summary>
        /// Thrown when a FIR channel's gain was tried to be accessed. There is no such thing, it's included in the filter coefficients.
        /// </summary>
        public FIRGainException() : base(message) { }
    }
}
