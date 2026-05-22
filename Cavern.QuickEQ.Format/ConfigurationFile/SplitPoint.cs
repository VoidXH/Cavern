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
        public readonly IFilterGraphNode[] Roots { get; }

        /// <summary>
        /// Represents a state in a <see cref="ConfigurationFile"/> which is a boundary of major steps (like crossover).
        /// </summary>
        public SplitPoint(string name, IFilterGraphNode[] roots) {
            Name = name;
            Roots = roots;
        }

        /// <summary>
        /// Get how many nodes are in this split.
        /// </summary>
        public int GetNodeCount() {
            HashSet<IFilterGraphNode> visited = new HashSet<IFilterGraphNode>();

            int CountChildren(IFilterGraphNode node) {
                int result = 0;
                for (int i = 0, c = node.Children.Count; i < c; i++) {
                    IFilterGraphNode child = node.Children[i];
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
            IFilterGraphNode[] roots = new IFilterGraphNode[Roots.Length];
            Dictionary<IFilterGraphNode, IFilterGraphNode> cloned = new Dictionary<IFilterGraphNode, IFilterGraphNode>();
            int clonedNodes = 0;
            for (int ch = 0; ch < Roots.Length; ch++) {
                roots[ch] = (IFilterGraphNode)Roots[ch].Clone();
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
        int CloneNode(IFilterGraphNode parent, IFilterGraphNode current, Dictionary<IFilterGraphNode, IFilterGraphNode> cloned) {
            if (cloned.ContainsKey(current)) {
                cloned[parent].AddChild(cloned[current]);
                return 0;
            }

            IFilterGraphNode clonedParent = cloned[parent];
            IFilterGraphNode clone = (IFilterGraphNode)current.Clone();
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
