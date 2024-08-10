using System;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Thrown when <see cref="IIRFilterSet.Bands"/> can't be used, because the band count is dependent on channels more than
    /// just having a different main/LFE band count. In this case, use <see cref="IIRFilterSet.GetBands(Channels.ReferenceChannel)"/>.
    /// </summary>
    public class ChannelDependentBandCountException : Exception {
        const string message = "The band count is dependent on the channel.";

        /// <summary>
        /// Thrown when <see cref="IIRFilterSet.Bands"/> can't be used, because the band count is dependent on channels more than
        /// just having a different main/LFE band count. In this case, use <see cref="IIRFilterSet.GetBands(Channels.ReferenceChannel)"/>.
        /// </summary>
        public ChannelDependentBandCountException() : base(message) { }
    }

    /// <summary>
    /// The target filter set couldn't be used for configuration without before/after measurements (target curve hacking),
    /// so regular filter set exports are not supported.
    /// </summary>
    public class DeltaSetException : Exception {
        const string message = "The target filter set couldn't be used for configuration without before/after measurements " +
            "(target curve hacking), so regular filter set exports are not supported.";

        /// <summary>
        /// The target filter set couldn't be used for configuration without before/after measurements (target curve hacking),
        /// so regular filter set exports are not supported.
        /// </summary>
        public DeltaSetException() : base(message) { }
    }

    /// <summary>
    /// Represents that this filter set was incorrectly initialized as a nonexistent or invalid file was read.
    /// </summary>
    public class InvalidSourceException : Exception { }

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