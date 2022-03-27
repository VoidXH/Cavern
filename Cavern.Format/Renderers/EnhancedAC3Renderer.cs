using Cavern.Format.Decoders;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded E-AC-3 stream with Cavern.
    /// </summary>
    public class EnhancedAC3Renderer : Renderer {
        /// <summary>
        /// Source stream.
        /// </summary>
        readonly EnhancedAC3Decoder stream;

        /// <summary>
        /// Parse an E-AC-3 decoder to a renderer.
        /// </summary>
        public EnhancedAC3Renderer(EnhancedAC3Decoder stream) => this.stream = stream;

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        public override void Update(int samples) {
            float[] input = new float[samples * stream.ChannelCount];
            stream.DecodeBlock(input, 0, input.LongLength);
            // TODO: render, position
        }
    }
}