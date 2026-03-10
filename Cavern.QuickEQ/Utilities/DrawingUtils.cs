using Cavern.Numerics;
using System;

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
            for (int y = 0; y < position.Height; y++) {
                for (int x = 0; x < position.Width; x++) {
                    image[pos + x] = color;
                }
                pos += imageWidth;
            }
        }

        /// <summary>
        /// Draw a colored rectangle outline on the image in production.
        /// </summary>
        public static void AddRectangle(uint[] image, int imageWidth, Rectangle position, int outline, uint color) {
            int pos = position.Y * imageWidth + position.X;
            int outlineIn = Math.Min(outline, position.Height);
            int outlineOut = Math.Max(position.Height - outline, outlineIn);
            void FillLine(int y) {
                for (int x = 0; x < position.Width; x++) {
                    image[pos + x] = color;
                }
                pos += imageWidth;
            }
            for (int y = 0; y < outlineIn; y++) {
                FillLine(y);
            }
            for (int y = outlineIn; y < outlineOut; y++) {
                for (int x = 0; x < Math.Min(outline, position.Width); x++) {
                    image[pos + x] = color;
                }
                for (int x = Math.Max(position.Width - outline, 0); x < position.Width; x++) {
                    image[pos + x] = color;
                }
                pos += imageWidth;
            }
            for (int y = outlineOut; y < position.Height; y++) {
                FillLine(y);
            }
        }

        /// <summary>
        /// Draw a colored circle outline on the image in production.
        /// </summary>
        public static void AddCircle(uint[] image, int imageWidth, Circle circle, int outline, uint color) {
            int centerX = (int)(circle.Center.X + .5f);
            int centerY = (int)(circle.Center.Y + .5f);
            int radius = (int)(circle.Radius + .5f);
            int radiusSquaredMin = (int)((circle.Radius - outline) * (circle.Radius - outline) + .5f);
            int radiusSquaredMax = (int)(circle.Radius * circle.Radius + .5f);
            for (int y = centerY - radius; y < centerY + radius; y++) {
                for (int x = centerX - radius; x < centerX + radius; x++) {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distSquared = dx * dx + dy * dy;
                    if (radiusSquaredMin < distSquared && distSquared < radiusSquaredMax) {
                        image[y * imageWidth + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Draw a colored circle on the image in production.
        /// </summary>
        public static void AddCircle(uint[] image, int imageWidth, Circle circle, uint color) {
            int centerX = (int)(circle.Center.X + .5f);
            int centerY = (int)(circle.Center.Y + .5f);
            int radius = (int)(circle.Radius + .5f);
            int radiusSquared = (int)(circle.Radius * circle.Radius + .5f);
            for (int y = centerY - radius; y < centerY + radius; y++) {
                for (int x = centerX - radius; x < centerX + radius; x++) {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy < radiusSquared) {
                        image[y * imageWidth + x] = color;
                    }
                }
            }
        }
    }
}
