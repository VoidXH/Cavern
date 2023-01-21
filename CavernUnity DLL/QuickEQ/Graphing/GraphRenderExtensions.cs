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
            uint[] data = renderer.Pixels;
            Color[] colors = new Color[data.Length];
            for (int i = 0; i < data.Length; i++) {
                colors[i] = new Color32((byte)((data[i] >> 16) & 0xFF), (byte)((data[i] >> 8) & 0xFF), (byte)(data[i] & 0xFF),
                    (byte)(data[i] >> 24));
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}