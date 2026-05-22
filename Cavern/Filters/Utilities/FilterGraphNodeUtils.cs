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
        public static void ConvertToConvolution(this IEnumerable<IFilterGraphNode> rootNodes, int sampleRate, int filterLength) {
            HashSet<IFilterGraphNode> visited = new HashSet<IFilterGraphNode>();
            Queue<IFilterGraphNode> queue = new Queue<IFilterGraphNode>(rootNodes);
            while (queue.Count > 0) {
                IFilterGraphNode currentNode = queue.Dequeue();
                if (visited.Contains(currentNode)) {
                    continue;
                }

                if (!(currentNode.Filter is BypassFilter || currentNode.Filter.GetType() == typeof(FastConvolver))) {
                    ConvertToConvolution(currentNode, sampleRate, filterLength);
                }
                visited.Add(currentNode);
                foreach (IFilterGraphNode child in currentNode.Children) {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Creates a copy of the complete graph with no overlapping memory with the old <paramref name="rootNodes"/>.
        /// </summary>
        public static IEnumerable<IFilterGraphNode> DeepCopy(this IEnumerable<IFilterGraphNode> rootNodes) =>
            DeepCopyWithMapping(rootNodes).rootNodes;

        /// <summary>
        /// Creates a copy of the complete graph with no overlapping memory with the old <paramref name="rootNodes"/>,
        /// and also results which old root <see cref="IFilterGraphNode"/> maps to which new one.
        /// </summary>
        public static (IEnumerable<IFilterGraphNode> rootNodes, Dictionary<IFilterGraphNode, IFilterGraphNode> mapping)
            DeepCopyWithMapping(this IEnumerable<IFilterGraphNode> rootNodes) {
            Dictionary<IFilterGraphNode, IFilterGraphNode> mapping = new Dictionary<IFilterGraphNode, IFilterGraphNode>();

            IFilterGraphNode CopyNode(IFilterGraphNode source) {
                if (mapping.ContainsKey(source)) {
                    return mapping[source];
                }

                IFilterGraphNode copy = (IFilterGraphNode)source.Clone();
                mapping[source] = copy;
                foreach (IFilterGraphNode child in source.Children) {
                    copy.AddChild(CopyNode(child));
                }
                return copy;
            }

            List<IFilterGraphNode> result = new List<IFilterGraphNode>();
            foreach (IFilterGraphNode rootNode in rootNodes) {
                result.Add(CopyNode(rootNode));
            }
            return (result, mapping);
        }

        /// <summary>
        /// Check if the graph has cycles.
        /// </summary>
        /// <param name="rootNodes">All nodes which have no parents</param>
        public static bool HasCycles(this IEnumerable<IFilterGraphNode> rootNodes) {
            HashSet<IFilterGraphNode> visited = new HashSet<IFilterGraphNode>(),
                inProgress = new HashSet<IFilterGraphNode>();
            foreach (IFilterGraphNode node in rootNodes) {
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
        static void ConvertToConvolution(IFilterGraphNode node, int sampleRate, int filterLength) {
            float[] impulse = new float[filterLength];
            impulse[0] = 1;

            IFilterGraphNode downmergeUntil = node;
            while (true) {
                downmergeUntil.Filter.Process(impulse);
                if (downmergeUntil.Children.Count != 1 || downmergeUntil.Children[0].Parents.Count != 1 ||
                    downmergeUntil.Children[0].Filter is BypassFilter) {
                    break;
                }
                downmergeUntil = downmergeUntil.Children[0];
            }

            IFilterGraphNode[] newChildren = downmergeUntil.Children.ToArray();
            downmergeUntil.DetachChildren();
            node.Filter = new FastConvolver(impulse, sampleRate, 0);
            node.DetachChildren();
            for (int i = 0; i < newChildren.Length; i++) {
                node.AddChild(newChildren[i]);
            }
        }

        /// <summary>
        /// Starting from a single node, checks if the graph has cycles.
        /// </summary>
        static bool HasCycles(IFilterGraphNode currentNode, HashSet<IFilterGraphNode> visited, HashSet<IFilterGraphNode> inProgress) {
            if (inProgress.Contains(currentNode)) {
                return true;
            }
            if (visited.Contains(currentNode)) {
                return false;
            }

            inProgress.Add(currentNode);
            foreach (IFilterGraphNode child in currentNode.Children) {
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