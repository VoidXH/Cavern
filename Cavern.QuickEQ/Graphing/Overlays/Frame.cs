using System.ComponentModel;

namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Draws a frame of a given <see cref="width"/> over the graph.
    /// </summary>
    public class Frame : GraphOverlay {
        /// <summary>
        /// Line stroke width.
        /// </summary>
        protected readonly int width;

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
            this.width = width;
            this.color = color;
        }

        /// <summary>
        /// Adds the overlay to a graph.
        /// </summary>
        public override void DrawOn(GraphRenderer target) {
            // Edge rows
            uint[] pixels = target.Pixels;
            int until = target.Width * width;
            for (int i = 0; i < until; i++) {
                pixels[i] = color;
            }
            for (int i = pixels.Length - until; i < pixels.Length; i++) {
                pixels[i] = color;
            }

            // Edge columns
            for (int x = 0; x < width; x++) {
                int c = target.Height - width;
                for (int y = width; y < c; y++) {
                    pixels[y * target.Width + x] = color;
                }
                for (int y = width; y < c; y++) {
                    pixels[pixels.Length - y * target.Width - x] = color;
                }
            }
        }
    }
}