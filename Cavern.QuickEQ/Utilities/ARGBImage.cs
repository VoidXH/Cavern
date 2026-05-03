using System;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Stores an image with 8-bit ARGB pixels.
    /// </summary>
    public class ARGBImage {
        /// <summary>
        /// Number of pixels on the X axis.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Number of pixels on the Y axis.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Value of each pixel on this image.
        /// </summary>
        public uint[] Pixels { get; }

        /// <summary>
        /// Creates an empty image with 8-bit ARGB pixels.
        /// </summary>
        public ARGBImage(int width, int height) {
            Width = width;
            Height = height;
            Pixels = new uint[Width * Height];
        }

        /// <summary>
        /// Stores an image with 8-bit ARGB pixels.
        /// </summary>
        public ARGBImage(int width, int height, uint[] pixels) {
            if (pixels == null) {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (width * height != pixels.Length) {
                throw new ArgumentOutOfRangeException(nameof(pixels), "Pixel count mismatch.");
            }

            Width = width;
            Height = height;
            Pixels = pixels;
        }

        /// <summary>
        /// Draw this image over the <paramref name="target"/>, completely overwriting when the alpha is max, not touching the pixels when the alpha is 0.
        /// Pixels extending through the frame is allowed, they will be cut.
        /// </summary>
        /// <remarks>This implementation assumes the <paramref name="target"/> image is final, setting the alpha of overlapped pixels to the maximum.</remarks>
        public void DrawOver(ARGBImage target, int xOffset, int yOffset) {
            int startX = Math.Max(0, -xOffset);
            int endX = Math.Min(Width, target.Width - xOffset);
            int startY = Math.Max(0, -yOffset);
            int endY = Math.Min(Height, target.Height - yOffset);

            for (int y = startY; y < endY; y++) {
                int sourceRowOffset = y * Width;
                int targetRowOffset = (y + yOffset) * target.Width;
                for (int x = startX; x < endX; x++) {
                    uint sourcePixel = Pixels[sourceRowOffset + x];
                    uint alpha = (sourcePixel >> 24) & 0xFF;
                    if (alpha == 0) {
                        continue;
                    }

                    int targetIndex = targetRowOffset + (x + xOffset);
                    if (alpha == 0xFF) {
                        target.Pixels[targetIndex] = sourcePixel;
                        continue;
                    }

                    uint targetPixel = target.Pixels[targetIndex];
                    uint rbSource = sourcePixel & 0x00FF00FF;
                    uint gSource = sourcePixel & 0x0000FF00;
                    uint rbTarget = targetPixel & 0x00FF00FF;
                    uint gTarget = targetPixel & 0x0000FF00;

                    uint rb = ((((rbSource - rbTarget) * alpha) >> 8) + rbTarget) & 0x00FF00FF;
                    uint g = ((((gSource - gTarget) * alpha) >> 8) + gTarget) & 0x0000FF00;
                    target.Pixels[targetIndex] = 0xFF000000 | rb | g;
                }
            }
        }
    }
}
