using System;

using Cavern.Filters;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when an unsupported filter would be exported to a <see cref="ConfigurationFile"/>.
    /// </summary>
    public abstract class UnsupportedFilterForExportException : Exception {
        /// <summary>
        /// The filter not supported by Equalizer APO.
        /// </summary>
        public Filter Filter { get; }

        /// <summary>
        /// Thrown when an unsupported filter would be exported to a <see cref="ConfigurationFile"/>.
        /// </summary>
        public UnsupportedFilterForExportException(string message, Filter filter) : base(message) => Filter = filter;
    }

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