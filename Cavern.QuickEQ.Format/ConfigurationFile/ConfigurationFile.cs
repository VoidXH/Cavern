using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Full parsed setup of a freely configurable system-wide equalizer or audio processor software.
    /// </summary>
    public abstract class ConfigurationFile {
        /// <summary>
        /// Root nodes of each channel, start attaching their filters as a children chain.
        /// </summary>
        public (string name, FilterGraphNode root)[] InputChannels { get; }

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        public IReadOnlyList<(string name, FilterGraphNode[] roots)> SplitPoints { get; }

        /// <summary>
        /// Create an empty configuration file with the passed input channels.
        /// </summary>
        protected ConfigurationFile(string name, ReferenceChannel[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i].GetShortName(), new FilterGraphNode(new InputChannel(inputs[i])));
            }

            SplitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channel names/labels.
        /// </summary>
        protected ConfigurationFile(string name, string[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i], new FilterGraphNode(new InputChannel(inputs[i])));
            }

            SplitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Adds an entry to the <see cref="SplitPoints"/> with the current state of the configuration, creating new
        /// <see cref="InputChannel"/>s after each existing <see cref="OutputChannel"/>.
        /// </summary>
        /// <remarks>If you keep track of your currently handled output nodes, set them to their children,
        /// because new input nodes are created in this function.</remarks>
        protected void CreateNewSplitPoint(string name) {
            FilterGraphNode[] nodes =
                FilterGraphNodeUtils.MapGraph(InputChannels.Select(x => x.root)).Where(x => x.Filter is OutputChannel).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = nodes[i].AddChild(new InputChannel(((OutputChannel)nodes[i].Filter).Channel));
            }
            ((List<(string, FilterGraphNode[])>)SplitPoints).Add((name, nodes));
        }

        /// <summary>
        /// Remove as many merge nodes (null filters) as possible.
        /// </summary>
        protected void Optimize() {
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
        /// Recursive part of <see cref="Optimize()"/>.
        /// </summary>
        /// <returns>Optimization was done and the children of the passed <paramref name="node"/> was modified.
        /// This means the currently processed element was removed and new were added, so the loop counter shouldn't increase
        /// in the iteration where this function was called from.</returns>
        bool Optimize(FilterGraphNode node) {
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
    }
}