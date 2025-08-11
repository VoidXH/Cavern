using Cavern.Channels;
using Cavern.Format.Decoders;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded MLP stream with Cavern.
    /// </summary>
    public class MeridianLosslessPackingRenderer : Renderer {
        /// <summary>
        /// Renders a decoded MLP stream with Cavern.
        /// </summary>
        public MeridianLosslessPackingRenderer(MeridianLosslessPackingDecoder stream) : base(stream) {
            if (stream.FullChannelCount != 0) {
                SetupObjects(stream.FullChannelCount);
            } else {
                SetupChannels();
            }
        }

        /// <inheritdoc/>
        public override ReferenceChannel[] GetChannels() => ((MeridianLosslessPackingDecoder)stream).Beds;

        /// <inheritdoc/>
        public override void Update(int samples) {
            throw new System.NotImplementedException();
        }
    }
}
