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
    /// Thrown when a format that doesn't allow duplicate labels would be used to export duplicate labels.
    /// </summary>
    public class DuplicateLabelException : Exception {
        const string message = "This format expects all labels to differ. \"{0}\" was present more than once.";

        /// <summary>
        /// Thrown when a format that doesn't allow duplicate labels would be used to export duplicate labels.
        /// </summary>
        public DuplicateLabelException(string label) : base(string.Format(message, label)) { }
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

    /// <summary>
    /// Thrown when a channel was used before it was created.
    /// </summary>
    public class NotPreparedChannelException : Exception {
        const string message = "A channel was used before it was created: {0}.";

        /// <summary>
        /// Thrown when a channel was used before it was created.
        /// </summary>
        public NotPreparedChannelException(string chName) : base(string.Format(message, chName)) { }
    }

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