using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;
using Cavern.Format.ConfigurationFile.ConvolutionBoxFormat;
using Cavern.Format.FilterSet;
using Cavern.Format.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// A file format only supporting matrix mixing and convolution filters.
    /// </summary>
    public sealed class ConvolutionBoxFormatConfigurationFile : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => "cbf";

        /// <summary>
        /// Sample rate of the convolution filters used.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Convolution Box Format with the default convoltion length of
        /// 65536 samples.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(ConfigurationFile other, int sampleRate) : this(other, sampleRate, 65536) { }

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Convolution Box Format with a custom
        /// <paramref name="convolutionLength"/>.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(ConfigurationFile other, int sampleRate, int convolutionLength) : base(other) {
            this.sampleRate = sampleRate;
            MergeSplitPoints();
            Optimize();
            SplitPoints[0].roots.ConvertToConvolution(convolutionLength);
        }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(string name, int sampleRate, ReferenceChannel[] inputs) : base(name, inputs) {
            this.sampleRate = sampleRate;
            FinishEmpty(inputs);
        }

        /// <summary>
        /// Read a Convolution Box Format from a file.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(string path) : base(Path.GetFileNameWithoutExtension(path), Parse(path)) {
            // Dirty way of reading the sample rate after parsing
            using FileStream stream = File.OpenRead(path);
            stream.Position = 4;
            sampleRate = stream.ReadInt32();
        }

        /// <summary>
        /// Parses a Convolution Box Format file to a filter graph, returns its root nodes.
        /// </summary>
        /// <remarks>Merge nodes can be created, calling <see cref="ConfigurationFile.Optimize"/> is recommended.</remarks>
        static (string, FilterGraphNode)[] Parse(string path) {
            using FileStream stream = File.OpenRead(path);
            if (stream.ReadInt32() != magicNumber) {
                throw new SyncException();
            }
            stream.Position = 8; // Sample rate is read in the constructor
            int entries = stream.ReadInt32();
            List<(string name, FilterGraphNode root)> inputChannels = new List<(string, FilterGraphNode)>();
            Dictionary<int, FilterGraphNode> lastNodes = new Dictionary<int, FilterGraphNode>();
            FilterGraphNode GetChannel(int index) { // Get an actual channel's last node
                if (lastNodes.ContainsKey(index)) {
                    return lastNodes[index];
                }
                string name = "CH" + (index + 1);
                FilterGraphNode newChannel = new FilterGraphNode(new InputChannel(name));
                inputChannels.Add((name, newChannel));
                return newChannel;
            }

            for (int i = 0; i < entries; i++) {
                CBFEntry entry = CBFEntry.Read(stream);
                if (entry is MatrixEntry matrix) {
                    Dictionary<int, FilterGraphNode> createdNodes = new Dictionary<int, FilterGraphNode>();
                    for (int mix = 0, c = matrix.Mixes.Count; mix < c; mix++) {
                        (int source, int[] targets) = matrix.Mixes[mix];
                        for (int target = 0; target < targets.Length; target++) {
                            FilterGraphNode sourceNode = lastNodes.ContainsKey(source) ?
                                lastNodes[source] : (lastNodes[source] = GetChannel(source));
                            FilterGraphNode targetNode = createdNodes.ContainsKey(targets[target]) ?
                                createdNodes[targets[target]] : (createdNodes[targets[target]] = new FilterGraphNode(null));
                            sourceNode.AddChild(targetNode);
                        }
                    }
                    foreach (KeyValuePair<int, FilterGraphNode> node in createdNodes) {
                        lastNodes[node.Key] = node.Value;
                    }
                } else if (entry is ConvolutionEntry convolution) {
                    FilterGraphNode last = lastNodes.ContainsKey(convolution.Channel) ?
                        lastNodes[convolution.Channel] : GetChannel(convolution.Channel);
                    FastConvolver filter = new FastConvolver(convolution.Filter);
                    if (last.Filter == null) {
                        last.Filter = filter;
                    } else {
                        lastNodes[convolution.Channel] = last.AddChild(filter);
                    }
                }
            }

            for (int i = 0, c = inputChannels.Count; i < c; i++) {
                if (i >= 0) {
                    // TODO: many points make input or output channels from channels, create them from names instead
                    lastNodes[i].AddChild(new OutputChannel(((InputChannel)inputChannels[i].root.Filter).ChannelName));
                }
            }
            return inputChannels.ToArray();
        }

        /// <inheritdoc/>
        public override void Export(string path) {
            ValidateForExport();
            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            List<CBFEntry> entries = new List<CBFEntry>();

            MatrixEntry GetMixer() { // Get the last entry if it's a mixer of make a new and return that
                int count = entries.Count;
                if (count == 0 || !(entries[count - 1] is MatrixEntry result)) {
                    result = new MatrixEntry();
                    entries.Add(result);
                }
                return result;
            }

            int GetIndex(FilterGraphNode node) { // Get filter index by node
                for (int i = 0; i < exportOrder.Length; i++) {
                    if (exportOrder[i].node == node) {
                        return exportOrder[i].channel;
                    }
                }
                throw new KeyNotFoundException();
            }

            for (int i = 0; i < exportOrder.Length; i++) {
                int channel = exportOrder[i].channel;
                // TODO: fix the broken copy filter order
                IReadOnlyList<FilterGraphNode> parents = exportOrder[i].node.Parents;
                if (parents.Count > 1) {
                    GetMixer().Expand(parents.Select(x => GetIndex(x)).ToArray(), channel);
                }

                Filter filter = exportOrder[i].node.Filter;
                if (filter is FastConvolver fastConvolver) {
                    entries.Add(new ConvolutionEntry(channel, fastConvolver.Impulse));
                } else if (filter is Convolver convolver) {
                    entries.Add(new ConvolutionEntry(channel, convolver.Impulse));
                }

                IReadOnlyList<FilterGraphNode> children = exportOrder[i].node.Children;
                int totalChildren = children.Count;
                if (totalChildren > 1) {
                    GetMixer().Expand(channel, children
                        .Where(x => x.Parents.Count == 1) // When this is not the case, the parent copy above will happen
                        .Select(x => GetIndex(x)).ToArray());
                } else if (totalChildren == 1) {
                    int targetChannel = GetIndex(children[0]);
                    if (channel != targetChannel) {
                        GetMixer().Expand(channel, targetChannel);
                    }
                }
            }

            using FileStream file = File.Open(path, FileMode.Create);
            file.WriteAny(magicNumber);
            file.WriteAny(sampleRate);
            int count = entries.Count;
            file.WriteAny(count);
            for (int i = 0; i < count; i++) {
                entries[i].Write(file);
            }
        }

        /// <summary>
        /// Throw an <see cref="UnsupportedFilterException"/> if it's not a convolution or a merge point.
        /// </summary>
        void ValidateForExport() {
            foreach (FilterGraphNode node in SplitPoints[0].roots.MapGraph()) {
                if (!(node.Filter is BypassFilter) && !(node.Filter is Convolver) && !(node.Filter is FastConvolver)) {
                    throw new UnsupportedFilterException(node.Filter);
                }
            }
        }

        /// <summary>
        /// CBFM marker.
        /// </summary>
        const int magicNumber = 0x4D464243;
    }
}