using System;

using Cavern.Filters;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when an unsupported filter would be exported to a <see cref="ConfigurationFile"/>.
    /// </summary>
    public abstract class UnsupportedFilterForExportException : Exception {
        /// <summary>
        /// The filter not supported by the <see cref="ConfigurationFile"/>.
        /// </summary>
        public Filter Filter { get; }

        /// <summary>
        /// Thrown when an unsupported filter would be exported to a <see cref="ConfigurationFile"/>.
        /// </summary>
        public UnsupportedFilterForExportException(string message, Filter filter) : base(message) => Filter = filter;
    }
}
