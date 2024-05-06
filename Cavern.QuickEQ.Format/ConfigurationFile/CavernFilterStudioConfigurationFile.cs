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
            base(name, ChannelPrototype.GetStandardMatrix(channelCount)) {
            for (int i = 0; i < channelCount; i++) { // Output markers
                InputChannels[i].root.AddChild(new FilterGraphNode(new OutputChannel(InputChannels[i].name)));
            }
        }
    }
}