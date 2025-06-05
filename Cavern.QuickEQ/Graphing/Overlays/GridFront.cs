namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// A <see cref="Grid"/> that draws the grid over the image instead of behind it.
    /// </summary>
    public class GridFront : Grid {
        /// <summary>
        /// A <see cref="Grid"/> that draws the grid over the image instead of behind it.
        /// </summary>
        public GridFront(int borderWidth, int gridWidth, uint color, int xSteps, int ySteps) : base(borderWidth, gridWidth, color, xSteps, ySteps) { }

        /// <inheritdoc/>
        public override void DrawBehind(DrawableMeasurement target) { }

        /// <inheritdoc/>
        public override void DrawOn(DrawableMeasurement target) {
            base.DrawBehind(target);
            base.DrawOn(target);
        }
    }
}