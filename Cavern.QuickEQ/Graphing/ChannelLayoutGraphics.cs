using System;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Draws a channel layout as an overhead image.
    /// </summary>
    public class ChannelLayoutGraphics {
        /// <summary>
        /// Width of the resulting image in pixels.
        /// </summary>
        public int Width { get; set; } = 512;

        /// <summary>
        /// Height of the resulting image in pixels.
        /// </summary>
        public int Height { get; set; } = 512;

        /// <summary>
        /// Side length of a square marking a channel.
        /// </summary>
        public int ChannelWidth { get; set; } = 64;

        /// <summary>
        /// Width of the connection lines for speaker layers.
        /// </summary>
        public int ConnectionWidth { get; set; } = 4;

        /// <summary>
        /// ARGB color of the drawn channels.
        /// </summary>
        public uint ChannelColor { get; set; } = 0xFFFFFFFF;

        /// <summary>
        /// ARGB color of the connections of channels on the same height plane.
        /// </summary>
        public uint ConnectionColor { get; set; } = 0x7FFFFFFF;

        /// <summary>
        /// ARGB color of the pixels not used for drawing the layout.
        /// </summary>
        public uint BackgroundColor { get; set; }

        /// <summary>
        /// Draws a channel layout as an overhead image. The pixels are in ARGB.
        /// </summary>
        public uint[] Draw(Channel[] channels) {
            uint[] image = new uint[Width * Height];
            for (int i = 0; i < image.Length; i++) {
                image[i] = BackgroundColor;
            }

            float lastElevation = float.NaN; // This optimization is enough, as channels are usually grouped by elevation
            for (int i = 0; i < channels.Length; i++) {
                if (channels[i].LFE) {
                    continue; // LFE is drawn to the center
                }
                float elevation = channels[i].X;
                if (lastElevation != elevation) {
                    DrawConnection(image, elevation);
                    lastElevation = elevation;
                }
            }

            int workingWidth = Width - ChannelWidth;
            int workingHeight = Height - ChannelWidth;
            for (int i = 0; i < channels.Length; i++) {
                float xPadding, yPadding;
                if (channels[i].LFE) {
                    xPadding = .5f;
                    yPadding = .5f;
                } else {
                    float relativePadding = .5f - MathF.Abs(channels[i].X) * (1f / (90 * 2));
                    xPadding = channels[i].CubicalPos.X * relativePadding + .5f;
                    yPadding = channels[i].CubicalPos.Z * relativePadding + .5f;
                }
                Fill(image, ChannelColor, (int)(workingWidth * xPadding), (int)(workingHeight * yPadding), ChannelWidth, ChannelWidth);
            }

            return image;
        }

        /// <summary>
        /// Draws a rectangle which hosts all the channels on that elevation.
        /// </summary>
        void DrawConnection(uint[] image, float elevation) {
            float relativePadding = MathF.Abs(elevation) * (1f / (90 * 2));
            int extraPadding = (ChannelWidth >> 1) - (ConnectionWidth >> 1),
                hPadding = (int)((Width - (extraPadding << 1)) * relativePadding) + extraPadding,
                vPadding = (int)((Height - (extraPadding << 1)) * relativePadding) + extraPadding;
            Fill(image, ConnectionColor, hPadding, vPadding, ConnectionWidth, Height - (vPadding << 1)); // Left
            Fill(image, ConnectionColor, Width - hPadding - ConnectionWidth, vPadding, ConnectionWidth, Height - (vPadding << 1)); // Right
            Fill(image, ConnectionColor, hPadding, vPadding, Width - (hPadding << 1), ConnectionWidth); // Top
            Fill(image, ConnectionColor, hPadding, Height - vPadding - ConnectionWidth, Width - (hPadding << 1), ConnectionWidth); // Bottom
        }

        /// <summary>
        /// Draw a colored rectangle on the image in production.
        /// </summary>
        void Fill(uint[] image, uint color, int x, int y, int width, int height) {
            int pos = y * Width + x;
            for (int yAdd = 0; yAdd < height; yAdd++) {
                for (int xAdd = 0; xAdd < width; xAdd++) {
                    image[pos + xAdd] = color;
                }
                pos += Width;
            }
        }
    }
}