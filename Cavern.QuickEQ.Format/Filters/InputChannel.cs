using System.Globalization;

using Cavern.Channels;
using Cavern.Format.ConfigurationFile;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ.Format library and is not available in the Cavern library's Filters namespace,
    /// because it shall only be created by <see cref="ConfigurationFile"/>s.</remarks>
    public class InputChannel : EndpointFilter, ILocalizableToString {
        /// <summary>
        /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        protected internal InputChannel(ReferenceChannel channel) : base(channel, kind) { }

        /// <summary>
        /// Marks an input channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the beginning of the filter pipeline</param>
        protected internal InputChannel(string channel) : base(channel, kind) { }

        /// <inheritdoc/>
        public override object Clone() => Channel != ReferenceChannel.Unknown ? new InputChannel(Channel) : new InputChannel(ChannelName);

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) {
            string name = Channel != ReferenceChannel.Unknown ? Channel.GetShortName() : ChannelName;
            return culture.Name switch {
                "hu-HU" => name + " bemenet",
                _ => $"{name} {kind}",
            };
        }

        /// <summary>
        /// Kind of this endpoint.
        /// </summary>
        const string kind = "input";
    }
}