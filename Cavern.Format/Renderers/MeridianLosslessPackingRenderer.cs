using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers.BaseClasses;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded MLP stream with Cavern.
    /// </summary>
    public class MeridianLosslessPackingRenderer : Renderer, IMixedBedObjectRenderer {
        /// <summary>
        /// By default, the presentation with the most channels is selected. This forces a specific presentation index.
        /// </summary>
        public static int? ForcedPresentation;

        /// <summary>
        /// Renders a decoded MLP stream with Cavern.
        /// </summary>
        public MeridianLosslessPackingRenderer(MeridianLosslessPackingDecoder stream) : base(stream) {
            if (stream.TracksIn16CH != 0) {
                SetupObjects(stream.TracksIn16CH);
            } else {
                SetupChannels();
            }
        }

        /// <inheritdoc/>
        public override ReferenceChannel[] GetChannels() => ((MeridianLosslessPackingDecoder)stream).Beds;

        /// <inheritdoc/>
        public ReferenceChannel[] GetStaticChannels() => ((MeridianLosslessPackingDecoder)stream).Beds;

        /// <inheritdoc/>
        public override void Update(int samples) {
            throw new System.NotImplementedException();
        }
    }
}
