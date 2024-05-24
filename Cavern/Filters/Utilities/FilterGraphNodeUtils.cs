using System.Collections.Generic;
using System.Linq;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Special functions for handling <see cref="FilterGraphNode"/>s.
    /// </summary>
    public static class FilterGraphNodeUtils {
        /// <summary>
        /// Convert the filter graph's filters to convolutions, and merge chains together to a single filter.
        /// </summary>
        public static void ConvertToConvolution(IEnumerable<FilterGraphNode> rootNodes, int filterLength) {
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
        /// Check if the graph has cycles.
        /// </summary>
        /// <param name="rootNodes">All nodes which have no parents</param>
        public static bool HasCycles(IEnumerable<FilterGraphNode> rootNodes) {
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
        /// Get all nodes in a filter graph knowing the root nodes.
        /// </summary>
        /// <param name="rootNodes">All nodes which have no parents</param>
        public static HashSet<FilterGraphNode> MapGraph(IEnumerable<FilterGraphNode> rootNodes) {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>();
            Queue<FilterGraphNode> queue = new Queue<FilterGraphNode>(rootNodes);
            while (queue.Count > 0) {
                FilterGraphNode currentNode = queue.Dequeue();
                if (visited.Contains(currentNode)) {
                    continue;
                }

                visited.Add(currentNode);
                foreach (FilterGraphNode child in currentNode.Children) {
                    queue.Enqueue(child);
                }
            }

            return visited;
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