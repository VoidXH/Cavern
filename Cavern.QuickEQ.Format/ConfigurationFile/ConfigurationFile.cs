using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Full parsed setup of a freely configurable system-wide equalizer or audio processor software.
    /// </summary>
    public abstract class ConfigurationFile : IExportable {
        /// <summary>
        /// Root nodes of each channel, start attaching their filters as a children chain.
        /// </summary>
        public (string name, FilterGraphNode root)[] InputChannels { get; }

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        public IReadOnlyList<(string name, FilterGraphNode[] roots)> SplitPoints => splitPoints;

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        readonly List<(string name, FilterGraphNode[] roots)> splitPoints;

        /// <inheritdoc/>
        public abstract string FileExtension { get; }

        /// <summary>
        /// Copy constructor from any <paramref name="other"/> configuration file.
        /// </summary>
        protected ConfigurationFile(ConfigurationFile other) {
            Dictionary<FilterGraphNode, FilterGraphNode> mapping = other.InputChannels.GetItem2s().DeepCopyWithMapping().mapping;
            InputChannels = other.InputChannels.Select(x => (x.name, mapping[x.root])).ToArray();
            splitPoints = other.SplitPoints.Select(x => (x.name, x.roots.Select(x => mapping[x]).ToArray())).ToList();
        }

        /// <summary>
        /// Construct a configuration file from a complete filter graph, with references to its <paramref name="inputChannels"/>.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. Refer to the constructors of
        /// <see cref="CavernFilterStudioConfigurationFile"/> for how to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, (string name, FilterGraphNode root)[] inputChannels) {
            InputChannels = inputChannels;
            splitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channels.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. It's not done here as there might
        /// be an initial configuration. Refer to the code of <see cref="CavernFilterStudioConfigurationFile(string, ReferenceChannel[])"/>
        /// for how to add closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, ReferenceChannel[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i].GetShortName(), new FilterGraphNode(new InputChannel(inputs[i])));
            }

            splitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channel names/labels.
        /// </summary>
        /// <remarks>It's mandatory to have the corresponding output channels to close the split point. It's not done here as there might
        /// be an initial configuration. Refer to the code of <see cref="EqualizerAPOConfigurationFile(string, int)"/> for how to implement
        /// addition and finishing up with closing <see cref="OutputChannel"/>s.</remarks>
        protected ConfigurationFile(string name, string[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i], new FilterGraphNode(new InputChannel(inputs[i])));
            }

            splitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Export this configuration to a target file. The general formula for most formats is:
        /// <list type="bullet">
        ///     <item>Get the filters in exportable order with <see cref="GetExportOrder"/>. This guarantees that all filters will be
        ///     handled in an order where all their parents were already exported.</item>
        ///     <item>For each entry, the parent channel indices can be queried with <see cref="GetExportedParents"/>. Handling parent
        ///     connections shall be before exporting said filter, because the filter is between the parents and children.</item>
        /// </list>
        /// </summary>
        public abstract void Export(string path);

        /// <summary>
        /// Add a new split with a custom <paramref name="name"/> at a specific <paramref name="index"/> of <see cref="SplitPoints"/>.
        /// </summary>
        public void AddSplitPoint(int index, string name) {
            if (index != SplitPoints.Count) {
                FilterGraphNode[] start = (FilterGraphNode[])splitPoints[index].roots.Clone();
                for (int i = 0; i < start.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)start[i].Filter).Channel;
                    start[i] = start[i].AddAfterParents(new OutputChannel(channel)).AddAfterParents(new InputChannel(channel));
                }
                splitPoints.Insert(index, (name, start));
            } else {
                CreateNewSplitPoint(name);
                FilterGraphNode[] end = SplitPoints[^1].roots;
                for (int i = 0; i < end.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)end[i].Filter).Channel;
                    end[i].AddChild(new OutputChannel(channel));
                }
            }
        }

        /// <summary>
        /// Clears all filters in one of the <see cref="SplitPoints"/> by <paramref name="index"/>.
        /// </summary>
        public void ClearSplitPoint(int index) {
            if (index == SplitPoints.Count - 1) { // Last split can be cleared and replaced with new outputs
                FilterGraphNode[] roots = SplitPoints[index].roots;
                for (int i = 0; i < roots.Length; i++) {
                    roots[i].DetachChildren();
                    roots[i].AddChild(new OutputChannel(((InputChannel)roots[i].Filter).Channel));
                }
            } else { // General case: clear the children and use the next split to fetch the outputs
                FilterGraphNode[] roots = SplitPoints[index].roots,
                    next = SplitPoints[index + 1].roots;
                for (int i = 0; i < roots.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)roots[i].Filter).Channel;
                    FilterGraphNode equivalent = next.First(x => ((InputChannel)x.Filter).Channel == channel);

                    roots[i].DetachChildren();
                    equivalent.Parents[0].DetachParents();
                    roots[i].AddChild(equivalent.Parents[0]);
                }
            }
        }

        /// <summary>
        /// Clears all filters in one of the <see cref="SplitPoints"/> by <paramref name="name"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        public void ClearSplitPoint(string name) => ClearSplitPoint(GetSplitPointIndexByName(name));

        /// <summary>
        /// Remove any splits, leave only one continuous graph of filters.
        /// </summary>
        public void MergeSplitPoints() {
            int c = SplitPoints.Count;
            if (c <= 1) {
                return;
            }

            for (int i = 1; i < c; i++) {
                FilterGraphNode[] roots = SplitPoints[i].roots;
                for (int j = 0; j < roots.Length; j++) {
                    roots[j].Parents[0].DetachFromGraph(); // Output of the previous split
                    roots[j].DetachFromGraph(); // Input of the current split
                }
            }
            splitPoints.RemoveRange(1, SplitPoints.Count - 1);
        }

        /// <summary>
        /// Get the index of a given <paramref name="channel"/> in the configuration. This is the input and output it's wired to.
        /// </summary>
        public int GetChannelIndex(ReferenceChannel channel) {
            for (int i = 0; i < InputChannels.Length; i++) {
                if (((InputChannel)InputChannels[i].root.Filter).Channel == channel) {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        /// <summary>
        /// Get the node for a split point's (referenced with an <paramref name="index"/>) given <paramref name="channel"/>.
        /// </summary>
        public FilterGraphNode GetSplitPointRoot(int index, ReferenceChannel channel) {
            FilterGraphNode[] roots = SplitPoints[index].roots;
            for (int i = 0; i < roots.Length; i++) {
                if (((InputChannel)roots[i].Filter).Channel == channel) {
                    return roots[i];
                }
            }
            throw new InvalidChannelException(new[] { channel });
        }

        /// <summary>
        /// Removes one of the <see cref="SplitPoints"/> by <paramref name="index"/> and clears all the filters it contains.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">The last split point can't be removed. To bypass this restriction,
        /// you could add an empty split point and remove the previous last one.</exception>
        public void RemoveSplitPoint(int index) {
            if (SplitPoints.Count == 1) {
                throw new IndexOutOfRangeException();
            }

            if (index == SplitPoints.Count - 1) { // Last split can be just removed
                FilterGraphNode[] roots = SplitPoints[index].roots;
                for (int i = 0; i < roots.Length; i++) {
                    roots[i].DetachFromGraph(false);
                }
            } else { // General case: transfer children from the next set of roots, then swap roots
                FilterGraphNode[] roots = SplitPoints[index].roots,
                    next = SplitPoints[index + 1].roots;
                for (int i = 0; i < roots.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)roots[i].Filter).Channel;
                    FilterGraphNode equivalent = next.First(x => ((InputChannel)x.Filter).Channel == channel);

                    roots[i].DetachChildren();
                    FilterGraphNode[] oldChildren = equivalent.Children.ToArray();
                    equivalent.DetachChildren();
                    roots[i].AddChildren(oldChildren);
                }
                splitPoints[index + 1] = (SplitPoints[index + 1].name, roots);
            }
            splitPoints.RemoveAt(index);
        }

        /// <summary>
        /// Removes one of the <see cref="SplitPoints"/> by <paramref name="name"/> and clears all the filters it contains.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        /// <exception cref="IndexOutOfRangeException">The last split point can't be removed. To bypass this restriction,
        /// you could add an empty split point and remove the previous last one.</exception>
        public void RemoveSplitPoint(string name) => RemoveSplitPoint(GetSplitPointIndexByName(name));

        /// <summary>
        /// Adds an entry to the <see cref="SplitPoints"/> with the current state of the configuration, creating new
        /// <see cref="InputChannel"/>s after each existing <see cref="OutputChannel"/>.
        /// </summary>
        /// <remarks>If you keep track of your currently handled output nodes, set them to their children,
        /// because new input nodes are created in this function.</remarks>
        protected void CreateNewSplitPoint(string name) {
            FilterGraphNode[] nodes = InputChannels.Select(x => x.root).MapGraph()
                .Where(x => x.Filter is OutputChannel && x.Children.Count == 0).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = nodes[i].AddChild(new InputChannel(((OutputChannel)nodes[i].Filter).Channel));
            }
            splitPoints.Add((name, nodes));
        }

        /// <summary>
        /// Add the neccessary <see cref="OutputChannel"/> entries for an empty configuration file.
        /// </summary>
        protected void FinishEmpty(ReferenceChannel[] channels) {
            for (int i = 0; i < channels.Length; i++) {
                InputChannels[i].root.AddChild(new FilterGraphNode(new OutputChannel(channels[i])));
            }
        }

        /// <summary>
        /// Get the nodes in topological order in which they can be exported to a linear description of the graph. This means that for
        /// each node that does a merge of two inputs (has multiple parents) will contain the full graph of its parents. The returned
        /// array has two values for each node: the node itself, and the channel index. This index can be negative: that means a virtual
        /// channel.
        /// </summary>
        protected (FilterGraphNode node, int channel)[] GetExportOrder() {
            List<FilterGraphNode> orderedNodes = InputChannels.GetItem2s().TopologicalSort();
            if (!orderedNodes.IsTopologicalSort()) {
                throw new DataMisalignedException();
            }

            (FilterGraphNode node, int channel)[] result = new (FilterGraphNode, int)[orderedNodes.Count];
            int lowestChannel = 0;
            for (int i = 0; i < result.Length; i++) {
                FilterGraphNode source = orderedNodes[i];
                int channelIndex;
                if (source.Children.Count == 0 && source.Filter is OutputChannel output) { // Actual exit node, not terminated virtual ch
                    channelIndex = GetChannelIndex(output.Channel);
                } else if (source.Parents.Count == 0) { // Entry node
                    channelIndex = GetChannelIndex(((InputChannel)source.Filter).Channel);
                } else { // TODO: greedily keep direct paths on the same channel index, don't have all filters on separate nodes
                    channelIndex = --lowestChannel;
                }
                result[i] = (source, channelIndex);
            }

            result.OptimizeChannelUse();
            return result;
        }

        /// <summary>
        /// Get the channels of the <see cref="FilterGraphNode"/> at a given <paramref name="index"/> in an <paramref name="exportOrder"/>
        /// created with <see cref="GetExportOrder"/>.
        /// </summary>
        protected int[] GetExportedParents((FilterGraphNode node, int channel)[] exportOrder, int index) =>
            exportOrder[index].node.Parents.Select(x => {
                for (int i = 0; i < exportOrder.Length; i++) {
                    if (exportOrder[i].node == x) {
                        return exportOrder[i].channel;
                    }
                }
                throw new KeyNotFoundException();
            }).ToArray();

        /// <summary>
        /// Remove as many merge nodes (null filters) as possible.
        /// </summary>
        protected void Optimize() {
            /// <summary>
            /// Recursive part of this function.
            /// </summary>
            /// <returns>Optimization was done and the children of the passed <paramref name="node"/> was modified.
            /// This means the currently processed element was removed and new were added, so the loop counter shouldn't increase
            /// in the iteration where this function was called from.</returns>
            static bool Optimize(FilterGraphNode node) {
                bool optimized = false;
                if (node.Filter == null) {
                    node.DetachFromGraph();
                    optimized = true;
                }

                IReadOnlyList<FilterGraphNode> children = node.Children;
                for (int i = 0, c = children.Count; i < c; i++) {
                    if (Optimize(children[i])) {
                        optimized = true;
                        i--;
                    }
                }
                return optimized;
            }

            for (int i = 0; i < InputChannels.Length; i++) {
                IReadOnlyList<FilterGraphNode> children = InputChannels[i].root.Children;
                for (int j = 0, c = children.Count; j < c;) {
                    if (!Optimize(children[j])) {
                        j++;
                    }
                }
            }
        }

        /// <summary>
        /// Get the index in the <see cref="SplitPoints"/> list by a split point's <paramref name="name"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        int GetSplitPointIndexByName(string name) {
            for (int i = 0, c = SplitPoints.Count; i < c; i++) {
                if (SplitPoints[i].name == name) {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}