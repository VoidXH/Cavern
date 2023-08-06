namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Draws a grid over the graph.
    /// </summary>
    public class Grid : Frame {
        /// <summary>
        /// Inner line stroke width.
        /// </summary>
        readonly int gridWidth;

        /// <summary>
        /// Number of columns drawn, including the frame lines.
        /// </summary>
        readonly int xSteps;

        /// <summary>
        /// Number of rows drawn, including the frame lines.
        /// </summary>
        readonly int ySteps;

        /// <summary>
        /// Draws a grid over the graph.
        /// </summary>
        /// <param name="borderWidth">Border line stroke width</param>
        /// <param name="gridWidth">Inner line stroke width</param>
        /// <param name="color">RGBA color of the line</param>
        /// <param name="xSteps">Number of columns drawn, including the frame lines</param>
        /// <param name="ySteps">Number of rows drawn, including the frame lines</param>
        public Grid(int borderWidth, int gridWidth, uint color, int xSteps, int ySteps) : base(borderWidth, color) {
            this.gridWidth = gridWidth;
            this.xSteps = xSteps;
            this.ySteps = ySteps;
        }

        /// <summary>
        /// Adds the overlay to a graph.
        /// </summary>
        public override void DrawBehind(GraphRenderer target) {
            int gap = target.Width / xSteps;
            for (int x = 1; x < xSteps; x++) {
                DrawColumn(target, x * gap, gridWidth, color);
            }

            gap = target.Height / ySteps;
            for (int y = 1; y < ySteps; y++) {
                DrawRow(target, y * gap, gridWidth, color);
            }
        }
    }
}