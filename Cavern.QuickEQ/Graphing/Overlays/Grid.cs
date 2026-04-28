using Cavern.Utilities;

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
        /// Number of intermediate columns drawn.
        /// </summary>
        readonly int xSteps;

        /// <summary>
        /// Number of intermediate rows drawn.
        /// </summary>
        readonly int ySteps;

        /// <summary>
        /// Draws a grid over the graph.
        /// </summary>
        /// <param name="borderWidth">Border line stroke width</param>
        /// <param name="gridWidth">Inner line stroke width</param>
        /// <param name="color">RGBA color of the line</param>
        /// <param name="xSteps">Number of intermediate columns drawn</param>
        /// <param name="ySteps">Number of intermediate rows drawn</param>
        public Grid(int borderWidth, int gridWidth, uint color, int xSteps, int ySteps) : base(borderWidth, color) {
            this.gridWidth = gridWidth;
            this.xSteps = xSteps;
            this.ySteps = ySteps;
        }

        /// <inheritdoc/>
        public override void DrawBehind(DrawableMeasurement target) {
            float gap = (float)target.Width / (xSteps + 1);
            for (int x = 1; x <= xSteps; x++) {
                DrawColumn(target, QMath.RoundToInt(x * gap), gridWidth, color);
            }

            gap = (float)target.Height / (ySteps + 1);
            for (int y = 1; y <= ySteps; y++) {
                DrawRow(target, QMath.RoundToInt(y * gap), gridWidth, color);
            }
        }
    }
}
