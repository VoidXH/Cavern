using Cavern.Channels;
using Cavern.Filters.Utilities;
using Cavern.Filters;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Cavern Filter Studio's own export format for full grouped filter pipelines.
    /// </summary>
    public class CavernFilterStudioConfigurationFile : ConfigurationFile {
        /// <summary>
        /// Create an empty file for a standard layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, int channelCount) :
            this(name, ChannelPrototype.GetStandardMatrix(channelCount)) { }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, params ReferenceChannel[] channels) :
            base(name, channels) {
            for (int i = 0; i < channels.Length; i++) { // Output markers
                InputChannels[i].root.AddChild(new FilterGraphNode(new OutputChannel(channels[i])));
            }
        }
    }
}