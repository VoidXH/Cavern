using Microsoft.Msagl.Drawing;

namespace FilterStudio.Graphs {
    /// <summary>
    /// An MSAGL <see cref="Edge"/> with display properties aligned for this application.
    /// </summary>
    public class StyledEdge {
        /// <summary>
        /// Edges can't be extended easily, because the <see cref="Graph"/> creates them. This is the <see cref="Edge"/> to handle on it.
        /// </summary>
        readonly Edge edge;

        /// <summary>
        /// Arrow and text color.
        /// </summary>
        public Color Foreground {
            get {
                return edge.Attr.Color;
            }
            set {
                edge.Attr.Color = value;
                if (edge.Label != null) {
                    edge.Label.FontColor = value;
                }
            }
        }

        /// <summary>
        /// An MSAGL <see cref="Edge"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="graph">Create the <see cref="edge"/> between the nodes of this <see cref="Graph"/></param>
        /// <param name="source">Unique ID of the source node</param>
        /// <param name="target">Unique ID of the target node</param>
        public StyledEdge(Graph graph, string source, string target) : this(graph, source, target, null, Color.White) { }

        /// <summary>
        /// An MSAGL <see cref="Edge"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="graph">Create the <see cref="edge"/> between the nodes of this <see cref="Graph"/></param>
        /// <param name="source">Unique ID of the source node</param>
        /// <param name="target">Unique ID of the target node</param>
        /// <param name="label">Displayed text next to the edge (null if not required)</param>
        public StyledEdge(Graph graph, string source, string target, string label) : this(graph, source, target, label, Color.White) { }

        /// <summary>
        /// An MSAGL <see cref="Edge"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="graph">Create the <see cref="edge"/> between the nodes of this <see cref="Graph"/></param>
        /// <param name="source">Unique ID of the source node</param>
        /// <param name="target">Unique ID of the target node</param>
        /// <param name="foreground">Edge and label color</param>
        public StyledEdge(Graph graph, string source, string target, Color foreground) : this(graph, source, target, null, foreground) { }

        /// <summary>
        /// An MSAGL <see cref="Edge"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="graph">Create the <see cref="edge"/> between the nodes of this <see cref="Graph"/></param>
        /// <param name="source">Unique ID of the source node</param>
        /// <param name="target">Unique ID of the target node</param>
        /// <param name="label">Displayed text next to the edge (null if not required)</param>
        /// <param name="foreground">Edge and label color</param>
        public StyledEdge(Graph graph, string source, string target, string label, Color foreground) {
            edge = graph.AddEdge(source, label, target);
            Foreground = foreground;
        }
    }
}