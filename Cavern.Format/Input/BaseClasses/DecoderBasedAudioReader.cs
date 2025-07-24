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

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            return CreateRenderer(decoder);
        }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            if (decoder == null) {
                decoder = CreateDecoder(skipSyncWord && reader.Position != 0);
                ChannelCount = decoder.ChannelCount;
                Length = decoder.Length;
                SampleRate = decoder.SampleRate;
                Bits = decoder.Bits;
            }
        }

        /// <summary>
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public override void Reset() {
            reader.Position = 0;
            if (decoder == null) {
                ReadHeader();
            }
            decoder.Seek(0);
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public override void Seek(long sample) {
            if (decoder == null) {
                ReadHeader();
            }
            decoder.Seek(sample);
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to);
    }
}
