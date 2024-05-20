using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Special functions for handling <see cref="FilterGraphNode"/>s.
    /// </summary>
    public static class FilterGraphNodeUtils {
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