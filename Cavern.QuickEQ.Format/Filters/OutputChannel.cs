using Cavern.Channels;
using Cavern.Format.ConfigurationFile;

namespace Cavern.Filters {
    /// <summary>
    /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ.Format library and is not available in the Cavern library's Filters namespace,
    /// because it shall only be created by <see cref="ConfigurationFile"/>s.</remarks>
    public class OutputChannel : BypassFilter {
        /// <inheritdoc/>
        public override bool LinearTimeInvariant => false;

        /// <summary>
        /// The channel for which this filter marks the end of the filter pipeline.
        /// </summary>
        public ReferenceChannel Channel { get; }

        /// <summary>
        /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the end of the filter pipeline</param>
        protected internal OutputChannel(ReferenceChannel channel) : base(channel.GetShortName() + " output") => Channel = channel;

        /// <summary>
        /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the end of the filter pipeline</param>
        protected internal OutputChannel(string channel) :
            base(ReferenceChannelExtensions.FromStandardName(channel).GetShortName() + " output") =>
            Channel = ReferenceChannelExtensions.FromStandardName(channel);
    }
}