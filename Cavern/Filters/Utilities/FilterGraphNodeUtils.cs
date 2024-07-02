using System.Collections.Generic;
using System.Linq;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Special functions for handling <see cref="FilterGraphNode"/>s.
    /// </summary>
    public static partial class FilterGraphNodeUtils {
        /// <summary>
        /// Convert the filter graph's filters to convolutions, and merge chains together to a single filter.
        /// </summary>
        public static void ConvertToConvolution(this IEnumerable<FilterGraphNode> rootNodes, int filterLength) {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>();
            Queue<FilterGraphNode> queue = new Queue<FilterGraphNode>(rootNodes);
            while (queue.Count > 0) {
                FilterGraphNode currentNode = queue.Dequeue();
                if (visited.Contains(currentNode)) {
                    continue;
                }

                if (!(currentNode.Filter is BypassFilter || currentNode.Filter.GetType() == typeof(FastConvolver))) {
                    ConvertToConvolution(currentNode, filterLength);
                }
                visited.Add(currentNode);
                foreach (FilterGraphNode child in currentNode.Children) {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Creates a copy of the complete graph with no overlapping memory with the old <paramref name="rootNodes"/>.
        /// </summary>
        public static IEnumerable<FilterGraphNode> DeepCopy(this IEnumerable<FilterGraphNode> rootNodes) =>
            DeepCopyWithMapping(rootNodes).rootNodes;

        /// <summary>
        /// Creates a copy of the complete graph with no overlapping memory with the old <paramref name="rootNodes"/>,
        /// and also results which old root <see cref="FilterGraphNode"/> maps to which new one.
        /// </summary>
        public static (IEnumerable<FilterGraphNode> rootNodes, Dictionary<FilterGraphNode, FilterGraphNode> mapping)
            DeepCopyWithMapping(this IEnumerable<FilterGraphNode> rootNodes) {
            Dictionary<FilterGraphNode, FilterGraphNode> mapping = new Dictionary<FilterGraphNode, FilterGraphNode>();

            FilterGraphNode CopyNode(FilterGraphNode source) {
                if (mapping.ContainsKey(source)) {
                    return mapping[source];
                }

                FilterGraphNode copy = new FilterGraphNode((Filter)source.Filter.Clone());
                mapping[source] = copy;
                foreach (var child in source.Children) {
                    copy.AddChild(CopyNode(child));
                }
                return copy;
            }

            List<FilterGraphNode> result = new List<FilterGraphNode>();
            foreach (var rootNode in rootNodes) {
                result.Add(CopyNode(rootNode));
            }
            return (result, mapping);
        }

        /// <summary>
        /// Check if the graph has cycles.
        /// </summary>
        /// <param name="rootNodes">All nodes which have no parents</param>
        public static bool HasCycles(this IEnumerable<FilterGraphNode> rootNodes) {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>(),
                inProgress = new HashSet<FilterGraphNode>();
            foreach (FilterGraphNode node in rootNodes) {
                if (!visited.Contains(node)) {
                    if (HasCycles(node, visited, inProgress)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Converts this filter to a convolution and upmerges all children until possible.
        /// </summary>
        static void ConvertToConvolution(FilterGraphNode node, int filterLength) {
            float[] impulse = new float[filterLength];
            impulse[0] = 1;

            FilterGraphNode downmergeUntil = node;
            while (true) {
                downmergeUntil.Filter.Process(impulse);
                if (downmergeUntil.Children.Count != 1 || downmergeUntil.Children[0].Parents.Count != 1 ||
                    downmergeUntil.Children[0].Filter is BypassFilter) {
                    break;
                }
                downmergeUntil = downmergeUntil.Children[0];
            }

            FilterGraphNode[] newChildren = downmergeUntil.Children.ToArray();
            downmergeUntil.DetachChildren();
            node.Filter = new FastConvolver(impulse);
            node.DetachChildren();
            for (int i = 0; i < newChildren.Length; i++) {
                node.AddChild(newChildren[i]);
            }
        }

        /// <summary>
        /// Starting from a single node, checks if the graph has cycles.
        /// </summary>
        static bool HasCycles(FilterGraphNode currentNode, HashSet<FilterGraphNode> visited, HashSet<FilterGraphNode> inProgress) {
            if (inProgress.Contains(currentNode)) {
                return true;
            }
            if (visited.Contains(currentNode)) {
                return false;
            }

            inProgress.Add(currentNode);
            foreach (FilterGraphNode child in currentNode.Children) {
                if (HasCycles(child, visited, inProgress)) {
                    return true;
                }
            }
            inProgress.Remove(currentNode);
            visited.Add(currentNode);
            return false;
        }
    }
}