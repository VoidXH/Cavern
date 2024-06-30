using Cavern.Channels;
using Cavern.Format.ConfigurationFile;

namespace Cavern.Filters {
    /// <summary>
    /// Handles a channel switching stages (like input and output).
    /// </summary>
    public abstract class EndpointFilter : BypassFilter {
        /// <summary>
        /// The name of the channel only, without the attached kind (<see cref="BypassFilter.Name"/> contains it to show it on labels).
        /// </summary>
        public string ChannelName { get; protected set; }

        /// <inheritdoc/>
        public override bool LinearTimeInvariant => false;

        /// <summary>
        /// The channel for which this filter marks the beginning of the filter pipeline.
        /// </summary>
        public ReferenceChannel Channel { get; }

        /// <summary>
        /// Marks an endpoint on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        /// <param name="kind">Type of this endpoint</param>
        internal EndpointFilter(ReferenceChannel channel, string kind) : base($"{channel.GetShortName()} {kind}") {
            Channel = channel;
            ChannelName = channel.GetShortName();
        }

        /// <summary>
        /// Marks an endpoint on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        /// <param name="kind">Type of this endpoint</param>
        internal EndpointFilter(string channel, string kind) : base($"{ParseName(channel)} {kind}") {
            Channel = ReferenceChannelExtensions.FromStandardName(channel);
            ChannelName = ParseName(channel);
        }

        /// <summary>
        /// If the <paramref name="channel"/> name is a shorthand for a channel, like an Equalizer APO label, try to get the full channel.
        /// </summary>
        static string ParseName(string channel) {
            ReferenceChannel standard = ReferenceChannelExtensions.FromStandardName(channel);
            if (standard != ReferenceChannel.Unknown) {
                return standard.GetShortName();
            } else {
                return channel;
            }
        }
    }
}