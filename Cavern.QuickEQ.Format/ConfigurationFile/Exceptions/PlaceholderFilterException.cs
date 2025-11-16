using System;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when a placeholder filter is used for signal processing.
    /// </summary>
    public class PlaceholderFilterException : Exception {
        const string message = "A placeholder filter is used for signal processing.";

        /// <summary>
        /// Thrown when a placeholder filter is used for signal processing.
        /// </summary>
        public PlaceholderFilterException() : base(message) { }
    }
}
