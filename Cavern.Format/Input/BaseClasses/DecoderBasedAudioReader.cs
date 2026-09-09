using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// <see cref="AudioReader"/> boilerplate for formats entirely implemented in a <see cref="Decoders.Decoder"/> and <see cref="Renderer"/>.
    /// </summary>
    public abstract class DecoderBasedAudioReader<TDecoder, TRenderer> : AudioReader
        where TDecoder : Decoder
        where TRenderer : Renderer {
        /// <inheritdoc/>
        public override long Position {
            get => Decoder.Position;
            set {
                if (Decoder == null) {
                    ReadHeader();
                }
                Decoder.Seek(value);
            }
        }

        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        protected TDecoder Decoder { get; private set; }

        /// <summary>
        /// The sync word from which the format is detected was already read from the stream - allows for format detection in
        /// streams that don't support <see cref="Stream.Position"/>.
        /// </summary>
        readonly bool skipSyncWord;

        /// <summary>
        /// <see cref="AudioReader"/> for formats entirely implemented in a <see cref="Decoders.Decoder"/> and <see cref="Renderer"/>.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        protected DecoderBasedAudioReader(Stream reader, bool skipSyncWord) : base(reader) => this.skipSyncWord = skipSyncWord;

        /// <summary>
        /// <see cref="AudioReader"/> for formats entirely implemented in a <see cref="Decoders.Decoder"/> and <see cref="Renderer"/>.
        /// </summary>
        /// <param name="path">Input file name</param>
        protected DecoderBasedAudioReader(string path) : base(path) { }

        /// <summary>
        /// Creates the <see cref="Decoder"/> instance.
        /// </summary>
        public abstract TDecoder CreateDecoder(bool skipSyncWord);

        /// <summary>
        /// Create a renderer for the given <paramref name="decoder"/>.
        /// </summary>
        public abstract TRenderer CreateRenderer(TDecoder decoder);

        /// <inheritdoc/>
        public sealed override Renderer GetRenderer() {
            if (Decoder == null) {
                ReadHeader();
            }
            return CreateRenderer(Decoder);
        }

        /// <inheritdoc/>
        public sealed override void ReadHeader() {
            if (Decoder == null) {
                Decoder = CreateDecoder(skipSyncWord && reader.Position != 0);
                ChannelCount = Decoder.ChannelCount;
                Length = Decoder.Length;
                SampleRate = Decoder.SampleRate;
                Bits = Decoder.Bits;
            }
        }

        /// <inheritdoc/>
        public sealed override void Reset() {
            if (Decoder == null) {
                reader.Position = 0;
                ReadHeader();
            } else if (Decoder.Position != 0) {
                Decoder.Seek(0);
            }
        }

        /// <inheritdoc/>
        public sealed override void ReadBlock(float[] samples, long from, long to) => Decoder.DecodeBlock(samples, from, to);
    }
}
