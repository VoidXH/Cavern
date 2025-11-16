using System;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// The target system does not support the applied filter.
    /// </summary>
    public class UnsupportedFilterException : Exception {
        const string message = "The target system does not support the applied filter";

        /// <summary>
        /// The target system does not support the applied filter.
        /// </summary>
        public UnsupportedFilterException() : base(message + '.') { }

        /// <summary>
        /// The target system does not support the applied filter.
        /// </summary>
        public UnsupportedFilterException(Filter filter) : base($"{message}: {filter}") { }
    }
}
