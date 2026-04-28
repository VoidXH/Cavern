using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Draws a grid over the graph.
    /// </summary>
    public class Grid : Frame {
        /// <summary>
        /// Number of intermediate columns drawn, additional to the borders.
        /// </summary>
        public int XSteps { get; }

        /// <summary>
        /// Number of intermediate rows drawn, additional to the borders.
        /// </summary>
        public int YSteps { get; }

        /// <summary>
        /// Inner line stroke width.
        /// </summary>
        readonly int gridWidth;

        /// <summary>
        /// Draws a grid over the graph.
        /// </summary>
        /// <param name="borderWidth">Border line stroke width</param>
        /// <param name="gridWidth">Inner line stroke width</param>
        /// <param name="color">RGBA color of the line</param>
        /// <param name="xSteps">Number of intermediate columns drawn, additional to the borders</param>
        /// <param name="ySteps">Number of intermediate rows drawn, additional to the borders</param>
        public Grid(int borderWidth, int gridWidth, uint color, int xSteps, int ySteps) : base(borderWidth, color) {
            XSteps = xSteps;
            YSteps = ySteps;
            this.gridWidth = gridWidth;
        }

        /// <inheritdoc/>
        public override void DrawBehind(DrawableMeasurement target) {
            float gap = (float)target.Width / (XSteps + 1);
            for (int x = 1; x <= XSteps; x++) {
                DrawColumn(target, QMath.RoundToInt(x * gap), gridWidth, color);
            }

            gap = (float)target.Height / (YSteps + 1);
            for (int y = 1; y <= YSteps; y++) {
                DrawRow(target, QMath.RoundToInt(y * gap), gridWidth, color);
            }
        }
    }
}
