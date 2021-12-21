using System;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Represents that this filter set was incorrectly initialized as a nonexistent or invalid file was read.
    /// </summary>
    public class InvalidSourceException : Exception { }

    /// <summary>
    /// The target system does not support the applied filter.
    /// </summary>
    public class UnsupportedFilterException : Exception { }
}