using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    public static partial class FilterGraphNodeUtils {
        /// <summary>
        /// When mapping a graph, decides the strategy about where to jump from a <paramref name="node"/>.
        /// </summary>
        delegate IReadOnlyList<IFilterGraphNode> MappingFunction(IFilterGraphNode node);

        /// <summary>
        /// Checks if a list of nodes is topologically sorted.
        /// </summary>
        public static bool IsTopologicalSort(this List<IFilterGraphNode> orderedNodes) {
            HashSet<IFilterGraphNode> visited = new HashSet<IFilterGraphNode>();
            for (int i = 0, c = orderedNodes.Count; i < c; i++) {
                IFilterGraphNode node = orderedNodes[i];
                IReadOnlyList<IFilterGraphNode> children = node.Children;
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
        public static HashSet<IFilterGraphNode> MapGraph(this IEnumerable<IFilterGraphNode> rootNodes) => MapGraph(rootNodes, node => node.Children);

        /// <summary>
        /// Get all nodes in a filter graph knowing the end nodes by discovering parents.
        /// </summary>
        /// <param name="rootNodes">All output nodes</param>
        public static HashSet<IFilterGraphNode> MapGraphBack(this IEnumerable<IFilterGraphNode> rootNodes) => MapGraph(rootNodes, node => node.Parents);

        /// <summary>
        /// Return all nodes on the graph in an order where every child is after its parents.
        /// </summary>
        public static List<IFilterGraphNode> TopologicalSort(this IFilterGraphNode[] rootNodes) {
            List<IFilterGraphNode> result = new List<IFilterGraphNode>();
            HashSet<IFilterGraphNode> visitedNodes = new HashSet<IFilterGraphNode>();

            void VisitNode(IFilterGraphNode node) {
                if (visitedNodes.Contains(node)) {
                    return;
                }
                visitedNodes.Add(node);
                foreach (IFilterGraphNode child in node.Children) {
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
        static HashSet<IFilterGraphNode> MapGraph(IEnumerable<IFilterGraphNode> rootNodes, MappingFunction mapping) {
            HashSet<IFilterGraphNode> visited = new HashSet<IFilterGraphNode>();
            Queue<IFilterGraphNode> queue = new Queue<IFilterGraphNode>(rootNodes);
            while (queue.Count > 0) {
                IFilterGraphNode currentNode = queue.Dequeue();
                if (visited.Contains(currentNode)) {
                    continue;
                }

                visited.Add(currentNode);
                foreach (IFilterGraphNode nextStep in mapping(currentNode)) {
                    queue.Enqueue(nextStep);
                }
            }

            return visited;
        }
    }
}