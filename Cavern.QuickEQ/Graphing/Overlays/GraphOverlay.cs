namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Something to draw over the graph, like a <see cref="Frame"/> or <see cref="Grid"/>.
    /// </summary>
    public abstract class GraphOverlay {
        /// <summary>
        /// Adds the overlay's foreground to a graph.
        /// </summary>
        public virtual void DrawOn(GraphRenderer target) { }

        /// <summary>
        /// Adds the overlay's background to a graph.
        /// </summary>
        public virtual void DrawBehind(GraphRenderer target) { }
    }
}