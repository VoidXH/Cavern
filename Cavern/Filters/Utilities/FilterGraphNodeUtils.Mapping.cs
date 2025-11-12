using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    public static partial class FilterGraphNodeUtils {
        /// <summary>
        /// Checks if a list of nodes is topologically sorted.
        /// </summary>
        public static bool IsTopologicalSort(this List<FilterGraphNode> orderedNodes) {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>();
            for (int i = 0, c = orderedNodes.Count; i < c; i++) {
                FilterGraphNode node = orderedNodes[i];
                IReadOnlyList<FilterGraphNode> children = node.Children;
                for (int j = 0, childCount = children.Count; j < childCount; j++) {
                    if (visited.Contains(children[j])) {
                        return false;
                    }
                }
                visited.Add(node);
            }
            return true;
        }

        /// <summary>
        /// Get all nodes in a filter graph knowing the root nodes.
        /// </summary>
        /// <param name="rootNodes">All nodes which have no parents</param>
        public static HashSet<FilterGraphNode> MapGraph(this IEnumerable<FilterGraphNode> rootNodes) {
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
        /// Return all nodes on the graph in an order where every child is after its parents.
        /// </summary>
        public static List<FilterGraphNode> TopologicalSort(this FilterGraphNode[] rootNodes) {
            List<FilterGraphNode> result = new List<FilterGraphNode>();
            HashSet<FilterGraphNode> visitedNodes = new HashSet<FilterGraphNode>();

            void VisitNode(FilterGraphNode node) {
                if (visitedNodes.Contains(node)) {
                    return;
                }
                visitedNodes.Add(node);
                foreach (FilterGraphNode child in node.Children) {
                    VisitNode(child);
                }
                result.Insert(0, node);
            }

            for (int i = 0; i < rootNodes.Length; i++) {
                VisitNode(rootNodes[i]);
            }
            return result;
        }
    }
}