using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

using Color = Microsoft.Msagl.Drawing.Color;

namespace FilterStudio.Graphs {
    /// <summary>
    /// The layout on which the steps of the filter pipeline can be selected. Each step has all input and output channels,
    /// they're just parts cut off the whole filter pipeline for better presentation. Think of them as groups on the full filter graph.
    /// The main feature this makes possible is having preset pipeline steps that can be added later with different configurations,
    /// such as crossovers.
    /// </summary>
    public class PipelineEditor : ManipulatableGraph {
        /// <summary>
        /// Pass the root nodes of the user's selected split.
        /// </summary>
        public event Action<FilterGraphNode[]> OnSplitChanged;

        /// <summary>
        /// Overrides the background color of the graph.
        /// </summary>
        public Color background;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        public ResourceDictionary language;

        /// <summary>
        /// The <see cref="ConfigurationFile"/> of which its split points will be presented.
        /// </summary>
        public ConfigurationFile Source {
            get => source;
            set {
                if (value == null) {
                    return;
                }
                source = value;
                RecreateGraph();
                SelectNode("0");
                OnSplitChanged?.Invoke(source.SplitPoints[0].roots);
            }
        }
        ConfigurationFile source;

        /// <summary>
        /// The layout on which the steps of the filter pipeline can be selected.
        /// </summary>
        public PipelineEditor() {
            OnLeftClick += LeftClick;
        }

        /// <summary>
        /// When the <see cref="Source"/> has changed, display its split points.
        /// </summary>
        void RecreateGraph() {
            IReadOnlyList<(string name, FilterGraphNode[] roots)> splits = source.SplitPoints;
            Graph graph = new Graph();
            graph.Attr.BackgroundColor = background;
            graph.Attr.LayerDirection = LayerDirection.LR;

            string lastUid = inNodeUid;
            graph.AddNode(new StyledNode(lastUid, (string)language["NInpu"]));
            for (int i = 0, c = splits.Count; i < c; i++) {
                string newUid = i.ToString();
                graph.AddNode(new StyledNode(newUid, splits[i].name));
                new StyledEdge(graph, lastUid, newUid);
                lastUid = newUid;
            }
            graph.AddNode(new StyledNode(outNodeUid, (string)language["NOutp"]));
            new StyledEdge(graph, lastUid, outNodeUid);
            Graph = graph;
        }

        /// <summary>
        /// Open the split the user selects.
        /// </summary>
        void LeftClick(object element) {
            if (element is not StyledNode node) {
                return;
            }

            if (int.TryParse(node.Id, out int root)) {
                (string _, FilterGraphNode[] roots) = source.SplitPoints[root];
                OnSplitChanged?.Invoke(roots);
            }
        }

        /// <summary>
        /// UID of the node that represents the filter set input.
        /// </summary>
        internal const string inNodeUid = "in";

        /// <summary>
        /// UID of the node that represents the filter set output.
        /// </summary>
        internal const string outNodeUid = "out";
    }
}