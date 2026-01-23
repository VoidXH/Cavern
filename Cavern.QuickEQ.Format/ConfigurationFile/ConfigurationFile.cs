using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;
using Cavern.Format.ConfigurationFile.Helpers;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Full parsed setup of a freely configurable system-wide equalizer or audio processor software.
    /// </summary>
    public abstract partial class ConfigurationFile : IExportable {
        /// <summary>
        /// Root nodes of each channel, start attaching their filters as a children chain. These nodes must contain
        /// <see cref="InputChannel"/> filters.
        /// </summary>
        public (string name, FilterGraphNode root)[] InputChannels { get; }

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        public IReadOnlyList<SplitPoint> SplitPoints => splitPoints;

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        readonly List<SplitPoint> splitPoints;

        /// <inheritdoc/>
        public abstract string FileExtension { get; }

        /// <summary>
        /// Recursive part of the <see cref="Optimize()"/> function.
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
        /// Get the index of a given <paramref name="channel"/> in the configuration. This is the input and output it's wired to.
        /// </summary>
        public int GetChannelIndex(EndpointFilter channel) {
            if (channel.Channel != ReferenceChannel.Unknown) { // Faster if the reference is available
                for (int i = 0; i < InputChannels.Length; i++) {
                    if (((InputChannel)InputChannels[i].root.Filter).Channel == channel.Channel) {
                        return i;
                    }
                }
            } else {
                for (int i = 0; i < InputChannels.Length; i++) {
                    if (((InputChannel)InputChannels[i].root.Filter).ChannelName == channel.ChannelName) {
                        return i;
                    }
                }
            }
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        /// <summary>
        /// Remove as many merge nodes (null filters) as possible.
        /// </summary>
        public void Optimize() {
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
        /// Convert the lazy loadable <see cref="Filter"/>s to their real counterparts in parallel.
        /// </summary>
        protected void FinishLazySetup(int fftCacheSize) {
            using FFTCachePool pool = new FFTCachePool(fftCacheSize);
            FilterGraphNode[] nodes = SplitPoints[0].Roots.MapGraph().Where(x => x.Filter is ILazyLoadableFilter).ToArray();
            Parallelizer.ForUnchecked(0, nodes.Length, i => {
                nodes[i].Filter = ((ILazyLoadableFilter)nodes[i].Filter).CreateFilter(pool);
            });
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
                    channelIndex = GetChannelIndex(output);
                } else if (source.Parents.Count == 0) { // Entry node
                    channelIndex = GetChannelIndex((InputChannel)source.Filter);
                } else {
                    channelIndex = --lowestChannel;
                }
                result[i] = (source, channelIndex);
            }

            OptimizeChannelUse(result);
            return result;
        }

        /// <summary>
        /// Get the <see cref="FilterGraphNode"/> indices in the <paramref name="exportOrder"/> of the parents of the node at the
        /// given <paramref name="index"/>.
        /// </summary>
        protected IEnumerable<int> GetExportedParentIndices((FilterGraphNode node, int channel)[] exportOrder, int index) =>
            exportOrder[index].node.Parents.Select(x => {
                for (int i = 0; i < exportOrder.Length; i++) {
                    if (exportOrder[i].node == x) {
                        return i;
                    }
                }
                throw new KeyNotFoundException();
            });

        /// <summary>
        /// Get the channels of the <see cref="FilterGraphNode"/> at a given <paramref name="index"/> in an <paramref name="exportOrder"/>
        /// created with <see cref="GetExportOrder"/>.
        /// </summary>
        protected int[] GetExportedParents((FilterGraphNode node, int channel)[] exportOrder, int index) =>
            GetExportedParentIndices(exportOrder, index).Select(x => exportOrder[x].channel).ToArray();
    }
}
