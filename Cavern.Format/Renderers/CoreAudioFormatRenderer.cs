using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers.CoreAudioFormat;
using Cavern.Format.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded Core Audio Format stream.
    /// </summary>
    public class CoreAudioFormatRenderer : PCMToObjectsRenderer {
        /// <summary>
        /// The first PCM tracks are these specific channels.
        /// </summary>
        readonly ReferenceChannel[] channels;

        /// <summary>
        /// Movement data for each dynamic object (bed channel object indices are null).
        /// </summary>
        readonly MovementTimeframe[][] movement;

        /// <summary>
        /// Renders a channel-based Core Audio Format stream.
        /// </summary>
        public CoreAudioFormatRenderer(Decoder stream) : base(stream) {
            SetupChannels();
            objectSamples[0] = new float[0];
        }

        /// <summary>
        /// Renders an object-based Core Audio Format stream (Dolby Atmos Master Format).
        /// </summary>
        internal CoreAudioFormatRenderer(Decoder stream, YAML rootSource, YAML metadataSource) : base(stream) {
            DolbyAtmosMasterRootFile root = new DolbyAtmosMasterRootFile(rootSource, Channels);
            channels = root.Channels;
            DolbyAtmosMasterMetadataFile metadata = new DolbyAtmosMasterMetadataFile(metadataSource, root.ObjectMapping);
            movement = metadata.Movement;
            SetupObjects(Channels);
            objectSamples[0] = new float[0];
        }

        /// <inheritdoc/>
        public override void Update(int samples) {
            base.Update(samples);
            // TODO: apply movement data from metadata
        }

        /// <inheritdoc/>
        public override ReferenceChannel[] GetChannels() => channels ?? base.GetChannels();
    }
}
