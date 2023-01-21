namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Something to draw over the graph, like a <see cref="Frame"/> or <see cref="Grid"/>.
    /// </summary>
    public abstract class GraphOverlay {
        /// <summary>
        /// Adds the overlay to a graph.
        /// </summary>
        public abstract void DrawOn(GraphRenderer target);
    }
}