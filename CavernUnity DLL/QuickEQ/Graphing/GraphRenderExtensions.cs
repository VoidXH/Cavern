using UnityEngine;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Unity extension functions for graph rendering.
    /// </summary>
    public static class GraphRenderExtensions {
        /// <summary>
        /// Copies the rendered graph to a Unity texture.
        /// </summary>
        public static Texture2D ConvertToTexture(this GraphRenderer renderer) {
            Texture2D texture = new Texture2D(renderer.Width, renderer.Height);
            ConvertToTexture(renderer, texture, new Color[renderer.Pixels.Length]);
            return texture;
        }

        /// <summary>
        /// Copies the rendered graph to an existing Unity texture and color cache the size of <paramref name="reusedTexture"/>'s Pixels.
        /// </summary>
        public static unsafe void ConvertToTexture(this GraphRenderer renderer, Texture2D reusedTexture, Color[] reusedColors) {
            fixed (uint* pData = renderer.Pixels)
            fixed (Color* pColors = reusedColors) {
                uint* data = pData, end = data + renderer.Pixels.Length;
                Color* color = pColors;
                while (data != end) {
                    *color++ = new Color32((byte)((*data >> 16) & 0xFF), (byte)((*data >> 8) & 0xFF), (byte)(*data & 0xFF),
                        (byte)(*data >> 24));
                    data++;
                }
            }
            reusedTexture.SetPixels(reusedColors);
            reusedTexture.Apply(false);
        }
    }
}