using System.Runtime.CompilerServices;

namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Draws a frame of a given <see cref="Width"/> over the graph.
    /// </summary>
    public class Frame : GraphOverlay {
        /// <summary>
        /// Line stroke width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// RGBA color of the line.
        /// </summary>
        protected readonly uint color;

        /// <summary>
        /// Draws a frame over the graph.
        /// </summary>
        /// <param name="width">Line stroke width</param>
        /// <param name="color">RGBA color of the line</param>
        public Frame(int width, uint color) {
            Width = width;
            this.color = color;
        }

        /// <summary>
        /// Adds the overlay to a graph.
        /// </summary>
        public override void DrawOn(GraphRenderer target) {
            DrawRow(target, 0, Width, color);
            DrawRow(target, target.Height - Width, Width, color);
            DrawColumn(target, 0, Width, color);
            DrawColumn(target, target.Width - Width, Width, color);
        }

        /// <summary>
        /// Draw a single row at a height <paramref name="offset"/> of a developing image.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawRow(GraphRenderer target, int offset, int width, uint color) {
            uint[] pixels = target.Pixels;
            while (width > 0) {
                for (int y = offset * target.Width, end = y + target.Width; y < end; y++) {
                    pixels[y] = color;
                }
                offset++;
                width--;
            }
        }

        /// <summary>
        /// Draw a single column at a width <paramref name="offset"/> of a developing image.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawColumn(GraphRenderer target, int offset, int width, uint color) {
            uint[] pixels = target.Pixels;
            while (width > 0) {
                for (int y = 0, pos = offset, step = target.Width; y < target.Height; y++) {
                    pixels[pos] = color;
                    pos += step;
                }
                offset++;
                width--;
            }
        }
    }
}