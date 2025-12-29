using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    public static partial class FilterGraphNodeUtils {
        /// <summary>
        /// When mapping a graph, decides the strategy about where to jump from a <paramref name="node"/>.
        /// </summary>
        delegate IReadOnlyList<FilterGraphNode> MappingFunction(FilterGraphNode node);

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
        public static HashSet<FilterGraphNode> MapGraph(this IEnumerable<FilterGraphNode> rootNodes) => MapGraph(rootNodes, node => node.Children);

        /// <summary>
        /// Get all nodes in a filter graph knowing the end nodes by discovering parents.
        /// </summary>
        /// <param name="rootNodes">All output nodes</param>
        public static HashSet<FilterGraphNode> MapGraphBack(this IEnumerable<FilterGraphNode> rootNodes) => MapGraph(rootNodes, node => node.Parents);

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

        /// <summary>
        /// Perform BFS with a custom <paramref name="mapping"/> direction from each node.
        /// </summary>
        static HashSet<FilterGraphNode> MapGraph(IEnumerable<FilterGraphNode> rootNodes, MappingFunction mapping) {
            HashSet<FilterGraphNode> visited = new HashSet<FilterGraphNode>();
            Queue<FilterGraphNode> queue = new Queue<FilterGraphNode>(rootNodes);
            while (queue.Count > 0) {
                FilterGraphNode currentNode = queue.Dequeue();
                if (visited.Contains(currentNode)) {
                    continue;
                }

                visited.Add(currentNode);
                foreach (FilterGraphNode nextStep in mapping(currentNode)) {
                    queue.Enqueue(nextStep);
                }
            }

            return visited;
        }
    }
}