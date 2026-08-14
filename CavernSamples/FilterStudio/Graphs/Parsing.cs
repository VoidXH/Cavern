using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Windows.Media;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

using Color = Microsoft.Msagl.Drawing.Color;

namespace FilterStudio.Graphs {
    /// <summary>
    /// Utilities for converting structures from Cavern to MSAGL.
    /// </summary>
    public static class Parsing {
        /// <summary>
        /// Parse a WPF background brush's color to MSAGL.
        /// </summary>
        public static Color ParseBackground(SolidColorBrush source) => new Color(source.Color.R, source.Color.G, source.Color.B);

        /// <summary>
        /// Convert a <see cref="ConfigurationFile"/>'s filter graph to an MSAGL <see cref="Graph"/>.
        /// </summary>
        /// <param name="rootNodes">Filter graph to convert, from <see cref="ConfigurationFile.InputChannels"/></param>
        public static Graph ParseConfigurationFile(IFilterGraphNode[] rootNodes) {
            Graph result = new();
            Dictionary<IFilterGraphNode, string> nodeIds = [];
            int nextNodeId = 0;
            for (int i = 0; i < rootNodes.Length; i++) {
                string uid = GetNodeId(rootNodes[i], nodeIds, ref nextNodeId);
                if (result.FindNode(uid) == null) {
                    result.AddNode(new StyledNode(uid, rootNodes[i].ToString()) {
                        Filter = rootNodes[i]
                    });
                }

                IReadOnlyList<IFilterGraphNode> children = rootNodes[i].Children;
                for (int j = 0, c = children.Count; j < c; j++) {
                    AddToGraph(uid, children[j], result, nodeIds, ref nextNodeId);
                }
            }
            return result;
        }

        /// <summary>
        /// Get an ID that remains the same whenever the same graph is traversed in the same order.
        /// </summary>
        static string GetNodeId(IFilterGraphNode source, Dictionary<IFilterGraphNode, string> nodeIds, ref int nextNodeId) {
            if (!nodeIds.TryGetValue(source, out string uid)) {
                uid = nextNodeId++.ToString();
                nodeIds.Add(source, uid);
            }
            return uid;
        }

        /// <summary>
        /// Recursively build a visual tree of filter graphs.
        /// </summary>
        /// <param name="parent">Unique identifier of the parent node</param>
        /// <param name="source">Next processed node</param>
        /// <param name="target">Graph to display the node on</param>
        static void AddToGraph(string parent, IFilterGraphNode source, Graph target, Dictionary<IFilterGraphNode, string> nodeIds, ref int nextNodeId) {
            string uid = GetNodeId(source, nodeIds, ref nextNodeId);
            if (target.FindNode(uid) == null) {
                StyledNode node = new StyledNode(uid, source.ToString()) {
                    Filter = source
                };
                target.AddNode(node);
            }

            foreach (Edge edge in target.Edges) {
                if (edge.Source == parent && edge.Target == uid) {
                    return; // Already displayed path
                }
            }

            new StyledEdge(target, parent, uid);

            if (source.Filter is OutputChannel) {
                return; // Filters after output channels are part of different splits
            }
            for (int i = 0, c = source.Children.Count; i < c; i++) {
                AddToGraph(uid, source.Children[i], target, nodeIds, ref nextNodeId);
            }
        }
    }
}
