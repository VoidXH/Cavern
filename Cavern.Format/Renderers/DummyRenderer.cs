using Cavern.Format.Decoders;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders silence.
    /// </summary>
    public class DummyRenderer : Renderer {
        /// <summary>
        /// Renders silence.
        /// </summary>
        public DummyRenderer(Decoder stream) : base(stream) {
            SetupChannels(stream.ChannelCount);
        }

        /// <summary>
        /// Renders silence.
        /// </summary>
        public override void Update(int samples) { }
    }
}