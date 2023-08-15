using UnityEngine;

using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Unity extension functions for graph rendering.
    /// </summary>
    public static class GraphRenderExtensions {
        /// <summary>
        /// Add a curve with a Unity color.
        /// </summary>
        public static RenderedCurve AddCurve(this GraphRenderer renderer, Equalizer curve, Color color) =>
            renderer.AddCurve(curve, color.ToARGB());

        /// <summary>
        /// Copies the rendered graph to a Unity texture.
        /// </summary>
        public static Texture2D ConvertToTexture(this GraphRenderer renderer) =>
            renderer.Pixels.ConvertToTexture(renderer.Width, renderer.Height);

        /// <summary>
        /// Copies a rendered image to a Unity texture.
        /// </summary>
        public static Texture2D ConvertToTexture(this uint[] image, int width, int height) {
            Texture2D texture = new Texture2D(width, height);
            ConvertToTexture(image, texture, new Color[image.Length]);
            return texture;
        }

        /// <summary>
        /// Copies the rendered graph to an existing Unity texture and color cache the size of <paramref name="reusedTexture"/>'s Pixels.
        /// </summary>
        public static void ConvertToTexture(this GraphRenderer renderer, Texture2D reusedTexture, Color[] reusedColors) =>
            ConvertToTexture(renderer.Pixels, reusedTexture, reusedColors);

        /// <summary>
        /// Copies a rendered image to an existing Unity texture and color cache the size of <paramref name="reusedTexture"/>'s Pixels.
        /// </summary>
        public static unsafe void ConvertToTexture(this uint[] source, Texture2D reusedTexture, Color[] reusedColors) {
            fixed (uint* pData = source)
            fixed (Color* pColors = reusedColors) {
                uint* data = pData, end = data + source.Length;
                Color* color = pColors;
                while (data != end) {
                    *color++ = new Color32((byte)((*data >> 16) & 0xFF), (byte)((*data >> 8) & 0xFF), (byte)(*data & 0xFF),
                        (byte)(*data >> 24));
                    data++;
                }
            }
            reusedTexture.SetPixels(reusedColors);
            reusedTexture.Apply();
        }
    }
}