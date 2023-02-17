using Cavern.Format.Decoders;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders silence.
    /// </summary>
    public class DummyRenderer : Renderer {
        /// <summary>
        /// Renders silence.
        /// </summary>
        public DummyRenderer(Decoder stream) : base(stream) => SetupChannels();

        /// <summary>
        /// Renders silence.
        /// </summary>
        public override void Update(int samples) {
            // Override the update code with nothing to save on performance
        }
    }
}