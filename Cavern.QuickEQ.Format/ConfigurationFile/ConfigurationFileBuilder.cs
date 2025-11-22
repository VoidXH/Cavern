using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile.Presets;
using Cavern.Format.FilterSet;
using Cavern.Remapping;

using static Cavern.Format.FilterSet.EqualizerFilterSet;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Takes an emptry <see cref="ConfigurationFile"/>, and allows linear appending of filters to the end of channel filter chains.
    /// </summary>
    public class ConfigurationFileBuilder {
        /// <summary>
        /// Number of physical channels processed by the configuration file.
        /// </summary>
        public int ChannelCount => file.InputChannels.Length;

        /// <summary>
        /// The <see cref="ConfigurationFile"/> to build linearly.
        /// </summary>
        readonly ConfigurationFile file;

        /// <summary>
        /// Access the last nodes by <see cref="ReferenceChannel"/>.
        /// </summary>
        readonly Dictionary<ReferenceChannel, FilterGraphNode> lastReferenceNodes;

        /// <summary>
        /// Access the last nodes by channel name.
        /// </summary>
        readonly Dictionary<string, FilterGraphNode> lastNamedNodes;

        /// <summary>
        /// Takes an emptry <see cref="ConfigurationFile"/>, and allows linear appending of filters to the end of channel filter chains.
        /// </summary>
        public ConfigurationFileBuilder(ConfigurationFile file) {
            this.file = file;
            lastReferenceNodes = file.InputChannels.ToDictionary(x => ((InputChannel)x.root.Filter).Channel, x => x.root);
            lastNamedNodes = file.InputChannels.ToDictionary(x => x.name, x => x.root);
        }

        /// <summary>
        /// Add a new <paramref name="filter"/> after all other filters to a given <paramref name="channel"/>.
        /// </summary>
        public void AddToChannel(ReferenceChannel channel, Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            lastReferenceNodes[channel].AddBeforeChildren(node);
            lastReferenceNodes[channel] = node;
        }

        /// <summary>
        /// Add a new <paramref name="filter"/> after all other filters to a <paramref name="name"/>d channel.
        /// </summary>
        public void AddToChannel(string name, Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            lastNamedNodes[name].AddBeforeChildren(node);
            lastNamedNodes[name] = node;
        }

        /// <summary>
        /// Add an existing <see cref="FilterSet.FilterSet"/>'s corrections for each corresponding channel.
        /// </summary>
        public void AddFilterSet(FilterSet.FilterSet set) {
            if (set is EqualizerFilterSet eqSet) {
                AddFilterSet(eqSet);
            } else {
                throw new NotImplementedException("Only EqualizerFilterSets are supported for configuration files.");
            }
        }

        /// <summary>
        /// Add an existing <see cref="EqualizerFilterSet"/>'s corrections for each corresponding channel.
        /// </summary>
        public void AddFilterSet(EqualizerFilterSet set) {
            for (int i = 0; i < set.ChannelCount; i++) {
                EqualizerChannelData filter = (EqualizerChannelData)set.Channels[i];
                ReferenceChannel channel = filter.reference;
                AddToChannel(channel, new Gain(filter.gain));
                AddToChannel(channel, new Delay(set.GetDelay(i), set.SampleRate));
                AddToChannel(channel, new GraphicEQ(filter.curve, set.SampleRate));
            }
        }

        /// <summary>
        /// Add a <see cref="Crossover"/> to the <see cref="ConfigurationFile"/>'s beginning.
        /// </summary>
        public void AddCrossoverToFront(CrossoverFilterSet set) {
            set.Add(file, 0);
        }

        /// <summary>
        /// Add a <see cref="SpatialRemapping"/> to the <see cref="ConfigurationFile"/>'s beginning.
        /// </summary>
        public void AddSpatialRemappingToFront(SpatialRemappingSource source) {
            if (source != SpatialRemappingSource.Off) {
                Channel[] content = source.ToLayout();
                SpatialRemappingExtensions.ToConfigurationFile(SpatialRemapping.GetMatrix(content), file);
            }
        }
    }
}
