using System.Globalization;
using System.Xml;

using Cavern.Channels;
using Cavern.Format.ConfigurationFile;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ.Format library and is not available in the Cavern library's Filters namespace,
    /// because it shall only be created by <see cref="ConfigurationFile"/>s.</remarks>
    public class OutputChannel : EndpointFilter, ILocalizableToString {
        /// <summary>
        /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the end of the filter pipeline</param>
        protected internal OutputChannel(ReferenceChannel channel) : base(channel, kind) { }

        /// <summary>
        /// Marks an output channel on a parsed <see cref="ConfigurationFile"/> graph.
        /// </summary>
        /// <param name="channel">The channel for which this filter marks the end of the filter pipeline</param>
        protected internal OutputChannel(string channel) : base(channel, kind) { }

        /// <summary>
        /// Create the corresponding <see cref="OutputChannel"/> for an <paramref name="input"/>.
        /// </summary>
        protected internal OutputChannel(InputChannel input) : base(input.ChannelName, kind) => Channel = input.Channel;

        /// <inheritdoc/>
        public override object Clone() => Channel != ReferenceChannel.Unknown ? new OutputChannel(Channel) : new OutputChannel(ChannelName);

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(OutputChannel));
            writer.WriteAttributeString(nameof(Name), Name);
            writer.WriteAttributeString(nameof(Channel), Channel.ToString());
            writer.WriteAttributeString(nameof(ChannelName), ChannelName);
            writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) {
            string name = Channel != ReferenceChannel.Unknown ? Channel.GetShortName() : ChannelName;
            return culture.Name switch {
                "hu-HU" => name + " kimenet",
                _ => $"{name} {kind}",
            };
        }

        /// <summary>
        /// Kind of this endpoint.
        /// </summary>
        const string kind = "output";
    }
}