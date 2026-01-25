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
        /// Get how many nodes are in this split.
        /// </summary>
        public int GetNodeCount() {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>();

            int CountChildren(FilterGraphNode node) {
                int result = 0;
                for (int i = 0, c = node.Children.Count; i < c; i++) {
                    FilterGraphNode child = node.Children[i];
                    if (visited.Contains(child)) {
                        continue;
                    }
                    visited.Add(child);
                    if (child.Filter is OutputChannel) {
                        result++;
                    } else {
                        result += 1 + CountChildren(child);
                    }
                }
                return result;
            }

            int result = 0;
            for (int i = 0; i < Roots.Length; i++) {
                result += 1 + CountChildren(Roots[i]);
            }
            return result;
        }

        /// <summary>
        /// Deep clone all filters from the split point root until the output nodes.
        /// </summary>
        public object Clone() {
            FilterGraphNode[] roots = new FilterGraphNode[Roots.Length];
            Dictionary<FilterGraphNode, FilterGraphNode> cloned = new Dictionary<FilterGraphNode, FilterGraphNode>();
            int clonedNodes = 0;
            for (int ch = 0; ch < Roots.Length; ch++) {
                roots[ch] = (FilterGraphNode)Roots[ch].Clone();    
                cloned[Roots[ch]] = roots[ch];
                clonedNodes++;
                for (int child = 0, c = Roots[ch].Children.Count; child < c; child++) {
                    clonedNodes += CloneNode(Roots[ch], Roots[ch].Children[child], cloned);
                }
            }

            if (clonedNodes == GetNodeCount()) {
                return new SplitPoint(Name, roots);
            } else {
                throw new InvalidOperationException("The wrong number of nodes was cloned.");
            }
        }

        /// <summary>
        /// Use DFS to clone nodes and connect them to their cloned parents.
        /// </summary>
        /// <returns>The number of cloned nodes.</returns>
        int CloneNode(FilterGraphNode parent, FilterGraphNode current, Dictionary<FilterGraphNode, FilterGraphNode> cloned) {
            if (cloned.ContainsKey(current)) {
                cloned[parent].AddChild(cloned[current]);
                return 0;
            }

            FilterGraphNode clonedParent = cloned[parent];
            FilterGraphNode clone = (FilterGraphNode)current.Clone();
            clonedParent.AddChild(clone);
            cloned[current] = clone;

            int newNodes = 1;
            if (current.Filter is OutputChannel) {
                return newNodes;
            }

            for (int i = 0, c = current.Children.Count; i < c; i++) {
                newNodes += CloneNode(current, current.Children[i], cloned);
            }
            return newNodes;
        }
    }
}
