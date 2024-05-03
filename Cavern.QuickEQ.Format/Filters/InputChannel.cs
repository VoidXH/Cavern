using Cavern.Channels;
using Cavern.Format.ConfigurationFile;

namespace Cavern.Filters {
    /// <summary>
    /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ.Format library and is not available in the Cavern library's Filters namespace,
    /// because it shall only be created by <see cref="ConfigurationFile"/>s.</remarks>
    public class InputChannel : BypassFilter {
        /// <inheritdoc/>
        public override bool LinearTimeInvariant => false;

        /// <summary>
        /// The channel for which this filter marks the beginning of the filter pipeline.
        /// </summary>
        public ReferenceChannel Channel { get; }

        /// <summary>
        /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        protected internal InputChannel(ReferenceChannel channel) : base(channel.GetShortName() + " input") => Channel = channel;

        /// <summary>
        /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        protected internal InputChannel(string channel) :
            base(ReferenceChannelExtensions.FromStandardName(channel).GetShortName() + " input") =>
            Channel = ReferenceChannelExtensions.FromStandardName(channel);
    }
}