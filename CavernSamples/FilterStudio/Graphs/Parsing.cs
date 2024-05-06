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
        public static Graph ParseConfigurationFile(FilterGraphNode[] rootNodes) {
            Graph result = new();
            for (int i = 0; i < rootNodes.Length; i++) {
                string uid = rootNodes[i].GetHashCode().ToString();
                result.AddNode(new StyledNode(uid, rootNodes[i].ToString()) {
                    Filter = rootNodes[i]
                });

                IReadOnlyList<FilterGraphNode> children = rootNodes[i].Children;
                for (int j = 0, c = children.Count; j < c; j++) {
                    AddToGraph(uid, children[j], result);
                }
            }
            return result;
        }

        /// <summary>
        /// Recursively build a visual tree of filter graphs.
        /// </summary>
        /// <param name="parent">Unique identifier of the parent node</param>
        /// <param name="source">Next processed node</param>
        /// <param name="target">Graph to display the node on</param>
        static void AddToGraph(string parent, FilterGraphNode source, Graph target) {
            string uid = source.GetHashCode().ToString();
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
                AddToGraph(uid, source.Children[i], target);
            }
        }
    }
}