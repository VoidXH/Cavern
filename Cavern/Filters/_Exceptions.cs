using System;

namespace Cavern.Filters {
    /// <summary>
    /// Tells if a property can only be used when the filter was created with a set sample rate.
    /// </summary>
    public class SampleRateNotSetException : Exception {
        const string message = "This property can only be used when the filter was created with a set sample rate.";

        /// <summary>
        /// Tells if a property can only be used when the filter was created with a set sample rate.
        /// </summary>
        public SampleRateNotSetException() : base(message) { }
    }
}