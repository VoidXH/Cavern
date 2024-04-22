using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Windows.Media;

using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

using Color = Microsoft.Msagl.Drawing.Color;

namespace FilterStudio.Graphs {
    /// <summary>
    /// Utilities for converting structures from Cavern to MSAGL.
    /// </summary>
    public class Parsing {
        /// <summary>
        /// Parse a WPF background brush's color to MSAGL.
        /// </summary>
        public static Color ParseBackground(SolidColorBrush source) => new Color(source.Color.R, source.Color.G, source.Color.B);

        /// <summary>
        /// Convert a <see cref="ConfigurationFile"/>'s filter graph to an MSAGL <see cref="Graph"/>.
        /// </summary>
        /// <param name="source">Filter graph to convert</param>
        /// <param name="background">Graph background color</param>
        public static Graph ParseConfigurationFile(ConfigurationFile source, Color background) {
            Graph result = new();
            result.Attr.BackgroundColor = background;

            (string name, FilterGraphNode root)[] rootNodes = source.InputChannels;
            for (int i = 0; i < rootNodes.Length; i++) {
                result.AddNode(new StyledNode(rootNodes[i].name, rootNodes[i].name));
                IReadOnlyList<FilterGraphNode> children = rootNodes[i].root.Children;
                for (int j = 0, c = children.Count; j < c; j++) {
                    AddToGraph(rootNodes[i].name, children[j], result);
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
            StyledNode node = new StyledNode(uid, source.ToString());
            target.AddNode(node);
            new StyledEdge(target, parent, uid);
            for (int i = 0, c = source.Children.Count; i < c; i++) {
                AddToGraph(uid, source.Children[i], target);
            }
        }
    }
}