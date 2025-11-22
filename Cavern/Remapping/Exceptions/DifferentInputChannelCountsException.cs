using System;

namespace Cavern.Remapping {
    /// <summary>
    /// Tells if a mixing matrix has different input channel counts for two outputs.
    /// </summary>
    public class DifferentInputChannelCountsException : Exception {
        const string message = "The mixing matrix has different input channel counts for two outputs.";

        /// <summary>
        /// Tells if a mixing matrix has different input channel counts for two outputs.
        /// </summary>
        public DifferentInputChannelCountsException() : base(message) { }
    }
}
