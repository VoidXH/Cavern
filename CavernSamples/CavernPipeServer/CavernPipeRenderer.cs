using System;
using System.IO;

using Cavern;
using Cavern.Format;
using Cavern.Format.Renderers;

namespace CavernPipeServer {
    /// <summary>
    /// Handles rendering of incoming audio content and special protocol additions/transformations.
    /// </summary>
    public class CavernPipeRenderer {
        /// <summary>
        /// Cavern rendering instance.
        /// </summary>
        readonly Listener listener;

        /// <summary>
        /// Allocated for the presumably constant size of reply messages.
        /// </summary>
        byte[] cache = [];

        /// <summary>
        /// Handles rendering of incoming audio content and special protocol additions/transformations.
        /// </summary>
        public CavernPipeRenderer(Stream stream) {
            listener = new Listener();
            AudioReader reader = AudioReader.Open(stream);
            Renderer renderer = reader.GetRenderer();
            listener.AttachSources(renderer.Objects);
        }

        /// <summary>
        /// Wait for enough input stream data and render the next set of samples, of which the count will be <see cref="Listener.UpdateRate"/> per channel.
        /// </summary>
        public byte[] Render() {
            float[] render = listener.Render();
            int packetBytesNeeded = render.Length * sizeof(float);
            if (cache.Length != packetBytesNeeded) {
                cache = new byte[packetBytesNeeded];
            }
            Buffer.BlockCopy(render, 0, cache, cache.Length - render.Length, render.Length * sizeof(float));
            return cache;
        }
    }
}