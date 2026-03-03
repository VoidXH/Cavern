using Cavern.Numerics;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Helper functions for interacting with uint ARGB pixel data.
    /// </summary>
    public static class DrawingUtils {
        /// <summary>
        /// Draw a colored rectangle on the image in production.
        /// </summary>
        public static void AddRectangle(uint[] image, int imageWidth, Rectangle position, uint color) {
            int pos = position.Y * imageWidth + position.X;
            for (int yAdd = 0; yAdd < position.Height; yAdd++) {
                for (int xAdd = 0; xAdd < position.Width; xAdd++) {
                    image[pos + xAdd] = color;
                }
                pos += imageWidth;
            }
        }
    }
}
