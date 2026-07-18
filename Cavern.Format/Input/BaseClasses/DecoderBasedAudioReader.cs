using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// <see cref="AudioReader"/> boilerplate for formats entirely implemented in a <see cref="Decoder"/> and <see cref="Renderer"/>.
    /// </summary>
    public abstract class DecoderBasedAudioReader<TDecoder, TRenderer> : AudioReader
        where TDecoder : Decoder
        where TRenderer : Renderer {
        /// <inheritdoc/>
        public override long Position {
            get => decoder.Position;
            set {
                if (decoder == null) {
                    ReadHeader();
                }
                decoder.Seek(value);
            }
        }

        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        TDecoder decoder;

        /// <summary>
        /// The sync word from which the format is detected was already read from the stream - allows for format detection in
        /// streams that don't support <see cref="Stream.Position"/>.
        /// </summary>
        readonly bool skipSyncWord;

        /// <summary>
        /// <see cref="AudioReader"/> for formats entirely implemented in a <see cref="Decoder"/> and <see cref="Renderer"/>.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        protected DecoderBasedAudioReader(Stream reader, bool skipSyncWord) : base(reader) => this.skipSyncWord = skipSyncWord;

        /// <summary>
        /// <see cref="AudioReader"/> for formats entirely implemented in a <see cref="Decoder"/> and <see cref="Renderer"/>.
        /// </summary>
        /// <param name="path">Input file name</param>
        protected DecoderBasedAudioReader(string path) : base(path) { }

        /// <summary>
        /// Creates the <see cref="decoder"/> instance.
        /// </summary>
        public abstract TDecoder CreateDecoder(bool skipSyncWord);

        /// <summary>
        /// Create a renderer for the given <paramref name="decoder"/>.
        /// </summary>
        public abstract TRenderer CreateRenderer(TDecoder decoder);

        /// <inheritdoc/>
        public sealed override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            return CreateRenderer(decoder);
        }

        /// <inheritdoc/>
        public sealed override void ReadHeader() {
            if (decoder == null) {
                decoder = CreateDecoder(skipSyncWord && reader.Position != 0);
                ChannelCount = decoder.ChannelCount;
                Length = decoder.Length;
                SampleRate = decoder.SampleRate;
                Bits = decoder.Bits;
            }
        }

        /// <inheritdoc/>
        public sealed override void Reset() {
            if (decoder == null) {
                reader.Position = 0;
                ReadHeader();
            } else if (decoder.Position != 0) {
                decoder.Seek(0);
            }
        }

        /// <inheritdoc/>
        public sealed override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to);
    }
}
