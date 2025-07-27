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
            metadata = new YAML(File.ReadAllText(path + ".metadata"));

            string audioPath = path + ".audio";
            if (File.Exists(audioPath)) {
                reader = File.OpenRead(audioPath);
                return;
            }
            if (path.Length > 6) {
                audioPath = path[..^6] + ".caf";
                if (File.Exists(audioPath)) {
                    reader = File.OpenRead(audioPath);
                    return;
                }
            }
            throw new FileNotFoundException("No audio file (.atmos.audio or .caf) was found.", path);
        }

        /// <inheritdoc/>
        public override CoreAudioFormatDecoder CreateDecoder(bool skipSyncWord) => new CoreAudioFormatDecoder(reader, skipSyncWord);

        /// <inheritdoc/>
        public override CoreAudioFormatRenderer CreateRenderer(CoreAudioFormatDecoder decoder) => new CoreAudioFormatRenderer(decoder, root, metadata);
    }
}
