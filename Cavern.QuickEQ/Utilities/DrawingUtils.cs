using System;
using System.Numerics;

using Cavern.Numerics;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Helper functions for interacting with uint ARGB pixel data.
    /// </summary>
    public static class DrawingUtils {
        /// <summary>
        /// Draw a colored rectangle on the image in production.
        /// </summary>
        public static void AddRectangle(this uint[] image, int imageWidth, Rectangle position, uint color) {
            RectangleSizeChecks(image, imageWidth, position);
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
        public static void AddRectangle(this uint[] image, int imageWidth, Rectangle position, int outline, uint color) {
            RectangleSizeChecks(image, imageWidth, position);
            int pos = position.Y * imageWidth + position.X;
            int outlineIn = Math.Min(outline, position.Height);
            int outlineOut = Math.Max(position.Height - outline, outlineIn);

            void FillCurrentLine() {
                for (int x = 0; x < position.Width; x++) {
                    image[pos + x] = color;
                }
                pos += imageWidth;
            }

            for (int y = 0; y < outlineIn; y++) {
                FillCurrentLine();
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
                FillCurrentLine();
            }
        }

        /// <summary>
        /// Draw a colored circle on the image in production.
        /// </summary>
        public static void AddCircle(this uint[] image, int imageWidth, Circle circle, uint color) {
            int centerX = QMath.RoundToInt(circle.Center.X);
            int centerY = QMath.RoundToInt(circle.Center.Y);
            int radius = QMath.RoundToInt(circle.Radius);
            int radiusSquared = QMath.RoundToInt(circle.Radius * circle.Radius);
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

        /// <summary>
        /// Draw a colored circle outline on the image in production.
        /// </summary>
        public static void AddCircle(this uint[] image, int imageWidth, Circle circle, int outline, uint color) {
            int centerX = QMath.RoundToInt(circle.Center.X);
            int centerY = QMath.RoundToInt(circle.Center.Y);
            int radius = QMath.RoundToInt(circle.Radius);
            int radiusSquaredMin = QMath.RoundToInt((circle.Radius - outline) * (circle.Radius - outline));
            int radiusSquaredMax = QMath.RoundToInt(circle.Radius * circle.Radius);
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
        /// Create a transparent background image of a circle.
        /// </summary>
        public static ARGBImage CreateCircleSprite(int radius, uint color) {
            int diameter = radius * 2;
            ARGBImage result = new ARGBImage(diameter, diameter);
            AddCircle(result.Pixels, result.Width, new Circle(new Vector2(radius, radius), radius), color);
            return result;
        }

        /// <summary>
        /// Create a transparent background image of a circle outline.
        /// </summary>
        public static ARGBImage CreateCircleSprite(int radius, int outline, uint color) {
            int diameter = radius * 2;
            ARGBImage result = new ARGBImage(diameter, diameter);
            AddCircle(result.Pixels, result.Width, new Circle(new Vector2(radius, radius), radius), outline, color);
            return result;
        }

        /// <summary>
        /// Flip the <paramref name="image"/> vertically.
        /// </summary>
        public static void FlipVertically(this uint[] image, int imageWidth) {
            int imageHeight = image.Length / imageWidth;
            uint[] tempLine = new uint[imageWidth];
            for (int i = 0; i < imageHeight / 2; i++) {
                int topRow = i * imageWidth;
                int bottomRow = (imageHeight - 1 - i) * imageWidth;
                Array.Copy(image, topRow, tempLine, 0, imageWidth);
                Array.Copy(image, bottomRow, image, topRow, imageWidth);
                Array.Copy(tempLine, 0, image, bottomRow, imageWidth);
            }
        }

        /// <summary>
        /// Check if a drawn <paramref name="rectangle"/> fits within the <paramref name="image"/> bounds.
        /// </summary>
        static void RectangleSizeChecks(uint[] image, int imageWidth, Rectangle rectangle) {
            if (rectangle.X < 0) {
                throw new ArgumentOutOfRangeException(nameof(rectangle), "Rectangle extends over the frame's left.");
            }
            if (rectangle.X + rectangle.Width > imageWidth) {
                throw new ArgumentOutOfRangeException(nameof(rectangle), "Rectangle extends over the frame's right.");
            }
            if (rectangle.Y < 0) {
                throw new ArgumentOutOfRangeException(nameof(rectangle), "Rectangle extends over the frame's top.");
            }
            if (rectangle.Y + rectangle.Height > image.Length / imageWidth) {
                throw new ArgumentOutOfRangeException(nameof(rectangle), "Rectangle extends over the frame's bottom.");
            }
        }
    }
}
