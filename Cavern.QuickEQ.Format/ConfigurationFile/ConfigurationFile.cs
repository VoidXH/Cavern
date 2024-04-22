using System.Collections.Generic;

using Cavern.Filters.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Full parsed setup of a freely configurable system-wide equalizer or audio processor software.
    /// </summary>
    public abstract class ConfigurationFile {
        /// <summary>
        /// Root nodes of each channel, start attaching their filters as a children chain.
        /// </summary>
        /// <remarks>The root node has a null filter, it's only used to mark in a single instance if the channel is
        /// processed on two separate pipelines from the root.</remarks>
        public (string name, FilterGraphNode root)[] InputChannels { get; }

        /// <summary>
        /// Create an empty configuration file with the passed input channel names/labels.
        /// </summary>
        public ConfigurationFile(string[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i], new FilterGraphNode(null));
            }
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
                IReadOnlyList<FilterGraphNode> parents = node.Parents;
                while (parents.Count != 0) {
                    parents[0].DetachChild(node, true);
                }
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