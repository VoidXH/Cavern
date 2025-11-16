using Cavern.Filters;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when an unsupported filter would be exported for Cavern Filter Studio.
    /// </summary>
    public class NotCavernFilterStudioFilterException : UnsupportedFilterForExportException {
        const string message = "Cavern Filter Studio's format does not support the following filter: ";

        /// <summary>
        /// Thrown when an unsupported filter would be exported for Cavern Filter Studio.
        /// </summary>
        public NotCavernFilterStudioFilterException(Filter filter) : base(message + filter, filter) { }
    }
}
