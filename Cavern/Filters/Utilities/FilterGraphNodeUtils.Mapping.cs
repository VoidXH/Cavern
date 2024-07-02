using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Channel slot optimization: two non-parallel virtual channels should only occupy one virtual channel, but at different times.
        /// </summary>
        /// <param name="mapping">Node - channel mapping to optimize, virtual channels take negative indices</param>
        public static void OptimizeChannelUse(this (FilterGraphNode node, int channel)[] mapping) {
            int virtualChannels = -mapping.Min(x => x.channel);

            // Partition channels to "time" intervals (mapping indices)
            (int channel, int first, int last)[] intervals = new (int, int, int)[virtualChannels];
            for (int i = 0; i < intervals.Length; i++) {
                intervals[i].channel = -1 - i;
                intervals[i].first = -1;
            }
            for (int i = 0; i < mapping.Length; i++) {
                int interval = -1 - mapping[i].channel;
                if (interval < 0) {
                    continue; // Virtual channels only
                }
                if (intervals[interval].first == -1) {
                    intervals[interval].first = i;
                }
                intervals[interval].last = i;
            }

            // True creation is when the channel was separated, and true disappearance is when the channel was merged back
            for (int i = 0; i < intervals.Length; i++) {
                FilterGraphNode firstNode = mapping[intervals[i].first].node,
                    lastNode = mapping[intervals[i].last].node;
                for (int j = 0; j < mapping.Length; j++) {
                    if (mapping[j].node.Children.Contains(firstNode)) {
                        intervals[i].first = j;
                    }
                    if (mapping[j].node.Parents.Contains(lastNode)) {
                        intervals[i].last = j; // Might never be merged, but it's not a problem, we get our thread back faster
                    }
                }
            }

            // Interval graph optimization problem
            Array.Sort(intervals, (a, b) => {
                int first = a.first.CompareTo(b.first);
                return first != 0 ? first : a.last.CompareTo(b.last);
            });

            List<int> channelUsedUntil = new List<int>();
            for (int i = 0; i < intervals.Length; i++) {
                int slots = channelUsedUntil.Count,
                    needFrom = intervals[i].first;
                bool handled = false;
                for (int slot = 0; slot < slots; slot++) {
                    if (channelUsedUntil[slot] <= needFrom) {
                        intervals[i].channel = -1 - slot;
                        channelUsedUntil[slot] = intervals[i].last;
                        handled = true;
                        break;
                    }
                }

                if (!handled) {
                    channelUsedUntil.Add(intervals[i].last);
                    intervals[i].channel = -channelUsedUntil.Count;
                }
            }
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