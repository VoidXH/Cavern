using System.IO;

using Cavern.Format.Consts;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Decodes and renders Meridian Lossless Packing files.
    /// </summary>
    public class MeridianLosslessPackingReader : DecoderBasedAudioReader<MeridianLosslessPackingDecoder, MeridianLosslessPackingRenderer> {
        /// <summary>
        /// Decodes and renders Meridian Lossless Packing files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public MeridianLosslessPackingReader(Stream reader) : this(reader, false) { }

        /// <summary>
        /// Decodes and renders Meridian Lossless Packing files.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public MeridianLosslessPackingReader(Stream reader, bool skipSyncWord) : base(reader, skipSyncWord) { }

        /// <summary>
        /// Decodes and renders Meridian Lossless Packing files.
        /// </summary>
        /// <param name="path">Input file name</param>
        public MeridianLosslessPackingReader(string path) : base(path) { }

        /// <inheritdoc/>
        public override MeridianLosslessPackingDecoder CreateDecoder(bool skipSyncWord) {
            if (skipSyncWord) {
                throw new System.NotImplementedException();
            }
            BlockBuffer<byte> buffer = BlockBuffer<byte>.CreateForConstantPacketSize(reader, FormatConsts.blockSize);
            return new MeridianLosslessPackingDecoder(buffer);
        }

        /// <inheritdoc/>
        public override MeridianLosslessPackingRenderer CreateRenderer(MeridianLosslessPackingDecoder decoder) => new MeridianLosslessPackingRenderer(decoder);
    }
}
