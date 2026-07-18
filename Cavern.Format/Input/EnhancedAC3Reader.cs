using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Enhanced AC-3 file reader.
    /// </summary>
    public class EnhancedAC3Reader : DecoderBasedAudioReader<EnhancedAC3Decoder, EnhancedAC3Renderer>, IMetadataSupplier {
        /// <summary>
        /// File size to calculate the content length from, assuming AC-3 is constant bitrate.
        /// </summary>
        protected readonly long fileSize;

        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        protected EnhancedAC3Decoder decoder;

        /// <summary>
        /// The sync word from which the format is detected, which already read from the stream - allows for format detection in
        /// streams that don't support <see cref="Stream.Position"/>. 0 if not applicable.
        /// </summary>
        readonly int skippedSyncWord;

        /// <summary>
        /// Enhanced AC-3 file reader.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        public EnhancedAC3Reader(Stream reader) : this(reader, 0) { }

        /// <summary>
        /// Enhanced AC-3 file reader.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        /// <param name="skippedSyncWord">The sync word from which the format is detected, which already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public EnhancedAC3Reader(Stream reader, int skippedSyncWord) : base(reader, skippedSyncWord != 0) {
            fileSize = GetFileSize(reader);
            this.skippedSyncWord = skippedSyncWord;
        }

        /// <summary>
        /// Enhanced AC-3 file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public EnhancedAC3Reader(string path) : base(path) => fileSize = GetFileSize(reader);

        /// <summary>
        /// Parse the stream/file length if possible for length calculation and seek support.
        /// </summary>
        static long GetFileSize(Stream reader) => reader.CanSeek ? reader.Length : -1;

        /// <inheritdoc/>
        public override EnhancedAC3Decoder CreateDecoder(bool skipSyncWord) {
            byte[] firstFetch = null;
            if (skippedSyncWord != 0) {
                firstFetch = BitConverter.GetBytes(skippedSyncWord);
            }
            BlockBuffer<byte> buffer = BlockBuffer<byte>.CreateForConstantPacketSize(reader, FormatConsts.blockSize, firstFetch);
            return new EnhancedAC3Decoder(buffer, fileSize);
        }

        /// <inheritdoc/>
        public override EnhancedAC3Renderer CreateRenderer(EnhancedAC3Decoder decoder) => new EnhancedAC3Renderer(decoder);

        /// <inheritdoc/>
        public ReadableMetadata GetMetadata() => decoder.GetMetadata();
    }
}
