﻿using System;
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
    public class EnhancedAC3Reader : AudioReader, IMetadataSupplier {
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
        public EnhancedAC3Reader(Stream reader, int skippedSyncWord) : base(reader) {
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

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            byte[] firstFetch = null;
            if (skippedSyncWord != 0) {
                firstFetch = BitConverter.GetBytes(skippedSyncWord);
            }
            BlockBuffer<byte> buffer = BlockBuffer<byte>.CreateForConstantPacketSize(reader, FormatConsts.blockSize, firstFetch);
            decoder = new EnhancedAC3Decoder(buffer, fileSize);
            ChannelCount = decoder.ChannelCount;
            Length = decoder.Length;
            SampleRate = decoder.SampleRate;
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to - from);

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            return new EnhancedAC3Renderer(decoder);
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public override void Seek(long sample) => decoder.Seek(sample);

        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => decoder.GetMetadata();
    }
}