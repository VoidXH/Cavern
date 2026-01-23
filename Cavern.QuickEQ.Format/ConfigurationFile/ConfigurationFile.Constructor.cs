using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    partial class ConfigurationFile {
        /// <summary>
        /// Copy constructor from any <paramref name="other"/> configuration file.
        /// </summary>
        protected ConfigurationFile(ConfigurationFile other) {
            Dictionary<FilterGraphNode, FilterGraphNode> mapping = other.InputChannels.GetItem2s().DeepCopyWithMapping().mapping;
            InputChannels = other.InputChannels.Select(x => (x.name, mapping[x.root])).ToArray();
            splitPoints = other.SplitPoints.Select(x => new SplitPoint(x.Name, x.Roots.Select(x => mapping[x]).ToArray())).ToList();
        }

        /// <summary>
        /// Construct a configuration file from a partial filter graph, a single <see cref="SplitPoint"/>.
        /// </summary>
        protected ConfigurationFile(SplitPoint splitPoint) {
            InputChannels = splitPoint.Roots.Select(x => (((InputChannel)x.Filter).Channel.GetShortName(), x)).ToArray();
            splitPoints = new List<SplitPoint> { (SplitPoint)splitPoint.Clone() };
        }

        /// <summary>
        /// Construct a configuration file from a complete filter graph, including splitting to <paramref name="splitPoints"/>.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. Refer to the constructors of
        /// <see cref="CavernFilterStudioConfigurationFile"/> for how to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(List<SplitPoint> splitPoints) {
            InputChannels = splitPoints[0].Roots.Select(x => (((InputChannel)x.Filter).Channel.GetShortName(), x)).ToArray();
            this.splitPoints = splitPoints;
        }

        /// <summary>
        /// Construct a configuration file from a complete filter graph, with references to its <paramref name="inputChannels"/>.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. Refer to the constructors of
        /// <see cref="CavernFilterStudioConfigurationFile"/> for how to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, (string name, FilterGraphNode root)[] inputChannels) {
            InputChannels = inputChannels;
            splitPoints = new List<SplitPoint> {
                new SplitPoint(name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file for a standard layout of the given channel count.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. It's not done here as there might
        /// be an initial configuration. Call <see cref="FinishEmpty()"/> at the end of your constructor to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, int channelCount) {
            InputChannels = new (string name, FilterGraphNode root)[channelCount];
            ReferenceChannel[] channels = ChannelPrototype.GetStandardMatrix(channelCount);
            for (int i = 0; i < channels.Length; i++) {
                InputChannels[i] = (channels[i].GetShortName(), new FilterGraphNode(new InputChannel(channels[i])));
            }

            splitPoints = new List<SplitPoint> {
                new SplitPoint(name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channels.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. It's not done here as there might
        /// be an initial configuration. Call <see cref="FinishEmpty(ReferenceChannel[])"/> at the end of your constructor
        /// to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, ReferenceChannel[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i].GetShortName(), new FilterGraphNode(new InputChannel(inputs[i])));
            }

            splitPoints = new List<SplitPoint> {
                new SplitPoint(name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channel names/labels.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. It's not done here as there might
        /// be an initial configuration. Call <see cref="FinishEmpty()"/> at the end of your constructor to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, string[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i], new FilterGraphNode(new InputChannel(inputs[i])));
            }

            splitPoints = new List<SplitPoint> {
                new SplitPoint(name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Add the neccessary <see cref="OutputChannel"/> entries for an empty configuration file.
        /// </summary>
        protected void FinishEmpty() {
            for (int i = 0; i < InputChannels.Length; i++) {
                InputChannel input = (InputChannel)InputChannels[i].root.Filter;
                InputChannels[i].root.AddChild(new FilterGraphNode(new OutputChannel(input)));
            }
        }

        /// <summary>
        /// Add the neccessary <see cref="OutputChannel"/> entries for an empty configuration file faster when the exact channels are known.
        /// </summary>
        protected void FinishEmpty(ReferenceChannel[] channels) {
            for (int i = 0; i < channels.Length; i++) {
                InputChannels[i].root.AddChild(new FilterGraphNode(new OutputChannel(channels[i])));
            }
        }
    }
}
