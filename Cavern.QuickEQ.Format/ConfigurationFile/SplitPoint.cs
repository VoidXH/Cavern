using System;
using System.Collections.Generic;

using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Represents a state in a <see cref="ConfigurationFile"/> which is a boundary of major steps (like crossover).
    /// </summary>
    public readonly struct SplitPoint : ICloneable {
        /// <summary>
        /// User-given title of the split.
        /// </summary>
        public readonly string Name { get; }

        /// <summary>
        /// For each channel, in the order of the <see cref="ConfigurationFile"/>'s channels, the corresponding <see cref="InputChannel"/> filter.
        /// </summary>
        public readonly FilterGraphNode[] Roots { get; }

        /// <summary>
        /// Represents a state in a <see cref="ConfigurationFile"/> which is a boundary of major steps (like crossover).
        /// </summary>
        public SplitPoint(string name, FilterGraphNode[] roots) {
            Name = name;
            Roots = roots;
        }

        /// <summary>
        /// Deep clone all filters from the split point root until the output nodes.
        /// </summary>
        public object Clone() {
            FilterGraphNode[] roots = new FilterGraphNode[Roots.Length];
            Dictionary<FilterGraphNode, FilterGraphNode> cloned = new Dictionary<FilterGraphNode, FilterGraphNode>();
            for (int ch = 0; ch < Roots.Length; ch++) {
                roots[ch] = (FilterGraphNode)Roots[ch].Clone();
                cloned[Roots[ch]] = roots[ch];
                for (int child = 0, c = Roots[ch].Children.Count; child < c; child++) {
                    CloneNode(Roots[ch], Roots[ch].Children[child], cloned);
                }
            }
            return new SplitPoint(Name, roots);
        }

        /// <summary>
        /// Use DFS to clone nodes and connect them to their cloned parents.
        /// </summary>
        void CloneNode(FilterGraphNode parent, FilterGraphNode current, Dictionary<FilterGraphNode, FilterGraphNode> cloned) {
            if (cloned.ContainsKey(current)) {
                cloned[parent].AddChild(cloned[current]);
                return;
            }

            FilterGraphNode clonedParent = cloned[parent];
            FilterGraphNode clone = (FilterGraphNode)current.Clone();
            clonedParent.AddChild(clone);
            cloned[current] = clone;

            if (current.Filter is OutputChannel) {
                return;
            }

            for (int i = 0, c = current.Children.Count; i < c; i++) {
                CloneNode(current, current.Children[i], cloned);
            }
        }
    }
}
