using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Decodes and renders Limitless Audio Format files.
    /// </summary>
    public class LimitlessAudioFormatReader : DecoderBasedAudioReader<LimitlessAudioFormatDecoder, LimitlessAudioFormatRenderer> {
        /// <summary>
        /// Decodes and renders Limitless Audio Format files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public LimitlessAudioFormatReader(Stream reader) : this(reader, false) { }

        /// <summary>
        /// Decodes and renders Limitless Audio Format files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public LimitlessAudioFormatReader(Stream reader, bool skipSyncWord) : base(reader, skipSyncWord) { }

        /// <summary>
        /// Decodes and renders Limitless Audio Format files.
        /// </summary>
        /// <param name="path">Input file name</param>
        public LimitlessAudioFormatReader(string path) : base(path) { }

        /// <inheritdoc/>
        public override LimitlessAudioFormatDecoder CreateDecoder(bool skipSyncWord) => new LimitlessAudioFormatDecoder(reader, skipSyncWord);

        /// <inheritdoc/>
        public override LimitlessAudioFormatRenderer CreateRenderer(LimitlessAudioFormatDecoder decoder) => new LimitlessAudioFormatRenderer(decoder);
    }
}
