using Cavern.Filters;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when an unsupported filter would be exported for Equalizer APO.
    /// </summary>
    public class NotEqualizerAPOFilterException : UnsupportedFilterForExportException {
        const string message = "Equalizer APO does not support the following filter: ";

        /// <summary>
        /// Thrown when an unsupported filter would be exported for Equalizer APO.
        /// </summary>
        public NotEqualizerAPOFilterException(Filter filter) : base(message + filter, filter) { }
    }
}
