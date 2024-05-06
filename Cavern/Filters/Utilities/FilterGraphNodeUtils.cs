using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Special functions for handling <see cref="FilterGraphNode"/>s.
    /// </summary>
    public static class FilterGraphNodeUtils {
        /// <summary>
        /// Get all nodes in a filter graph knowing the root nodes.
        /// </summary>
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
    }
}