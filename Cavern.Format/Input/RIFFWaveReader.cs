using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Decodes and renders RIFF WAVE files.
    /// </summary>
    public class RIFFWaveReader : DecoderBasedAudioReader<RIFFWaveDecoder, RIFFWaveRenderer> {
        /// <summary>
        /// Decodes and renders RIFF WAVE files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public RIFFWaveReader(Stream reader) : this(reader, false) { }

        /// <summary>
        /// Decodes and renders RIFF WAVE files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public RIFFWaveReader(Stream reader, bool skipSyncWord) : base(reader, skipSyncWord) { }

        /// <summary>
        /// Decodes and renders RIFF WAVE files.
        /// </summary>
        /// <param name="path">Input file name</param>
        public RIFFWaveReader(string path) : base(path) { }

        /// <inheritdoc/>
        public override RIFFWaveDecoder CreateDecoder(bool skipSyncWord) => new RIFFWaveDecoder(reader, skipSyncWord);

        /// <inheritdoc/>
        public override RIFFWaveRenderer CreateRenderer(RIFFWaveDecoder decoder) => new RIFFWaveRenderer(decoder);
    }
}
