using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Decodes and renders Dolby Atmos Master Format files.
    /// </summary>
    public class DolbyAtmosMasterFormatReader : DecoderBasedAudioReader<CoreAudioFormatDecoder, CoreAudioFormatRenderer> {
        /// <summary>
        /// Root object containing channel/object mapping.
        /// </summary>
        readonly YAML root;

        /// <summary>
        /// Object movement data.
        /// </summary>
        readonly YAML metadata;

        /// <summary>
        /// Decodes and renders Dolby Atmos Master Format files.
        /// </summary>
        public DolbyAtmosMasterFormatReader(string path) : base(null, false) {
            root = new YAML(File.ReadAllText(path));
            reader = File.OpenRead(path + ".audio");
            metadata = new YAML(File.ReadAllText(path + ".metadata"));
        }

        /// <inheritdoc/>
        public override CoreAudioFormatDecoder CreateDecoder(bool skipSyncWord) => new CoreAudioFormatDecoder(reader, skipSyncWord);

        /// <inheritdoc/>
        public override CoreAudioFormatRenderer CreateRenderer(CoreAudioFormatDecoder decoder) => new CoreAudioFormatRenderer(decoder, root, metadata);
    }
}
