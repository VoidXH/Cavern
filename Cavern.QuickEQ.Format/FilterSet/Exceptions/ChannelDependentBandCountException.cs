using System;

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
}
