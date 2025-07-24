using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Decodes and renders Core Audio Format files.
    /// </summary>
    public class CoreAudioFormatReader : DecoderBasedAudioReader<CoreAudioFormatDecoder, CoreAudioFormatRenderer> {
        /// <summary>
        /// Decodes and renders Core Audio Format files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public CoreAudioFormatReader(Stream reader) : this(reader, false) { }

        /// <summary>
        /// Decodes and renders Core Audio Format files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public CoreAudioFormatReader(Stream reader, bool skipSyncWord) : base(reader, skipSyncWord) { }

        /// <summary>
        /// Decodes and renders Core Audio Format files.
        /// </summary>
        /// <param name="path">Input file name</param>
        public CoreAudioFormatReader(string path) : base(path) { }

        /// <inheritdoc/>
        public override CoreAudioFormatDecoder CreateDecoder(bool skipSyncWord) => new CoreAudioFormatDecoder(reader, skipSyncWord);

        /// <inheritdoc/>
        public override CoreAudioFormatRenderer CreateRenderer(CoreAudioFormatDecoder decoder) => new CoreAudioFormatRenderer(decoder);
    }
}
