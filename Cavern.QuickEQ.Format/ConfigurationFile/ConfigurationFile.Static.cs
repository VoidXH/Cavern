using System;
using System.Collections.Generic;

using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Cavern.Format.ConfigurationFile {
    partial class ConfigurationFile {
        /// <summary>
        /// Create a <see cref="ConfigurationFile"/> of a supported <paramref name="type"/>.
        /// </summary>
        public static ConfigurationFile Create(ConfigurationFileType type, string name, int sampleRate, int channelCount) => type switch {
            ConfigurationFileType.CavernFilterStudio => new CavernFilterStudioConfigurationFile(name, channelCount),
            ConfigurationFileType.ConvolutionBoxFormat => new ConvolutionBoxFormatConfigurationFile(name, sampleRate, channelCount),
            ConfigurationFileType.EqualizerAPO => new EqualizerAPOConfigurationFile(name, channelCount, true),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Create a <see cref="ConfigurationFile"/> of a supported <paramref name="type"/>, using a <see cref="FilterSet.FilterSet"/> as a <paramref name="source"/>.
        /// </summary>
        public static ConfigurationFile Create(ConfigurationFileType type, string name, int sampleRate, FilterSet.FilterSet source) {
            ConfigurationFile file = Create(type, name, sampleRate, source.ChannelCount);
            ConfigurationFileBuilder builder = new ConfigurationFileBuilder(file);
            builder.AddFilterSet(source);
            return file;
        }

        /// <summary>
        /// When generating a <paramref name="mapping"/>, where each node has their own virtual channel,
        /// merge them to a single virtual channel if they form a single line in the graph.
        /// </summary>
        /// <param name="mapping">Node - channel mapping to optimize, virtual channels take negative indices</param>
        protected static void OptimizeChannelUse((FilterGraphNode node, int channel)[] mapping) {
            for (int i = 0; i < mapping.Length; i++) {
                IReadOnlyList<FilterGraphNode> children = mapping[i].node.Children;
                if (children.Count == 1) {
                    FilterGraphNode child = children[0];
                    if (child.Parents.Count == 1 && // A straight line leads to this child and
                        !(child.Filter is OutputChannel && child.Children.Count == 0)) { // it's not a final output
                        for (int j = i + 1; j < mapping.Length; j++) {
                            if (mapping[j].node == child) {
                                mapping[j].channel = mapping[i].channel;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
