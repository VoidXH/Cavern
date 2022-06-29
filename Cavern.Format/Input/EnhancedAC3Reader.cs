﻿using System.IO;
using Cavern.Format.Consts;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Enhanced AC-3 file reader.
    /// </summary>
    public class EnhancedAC3Reader : AudioReader {
        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        EnhancedAC3Decoder decoder;

        /// <summary>
        /// Enhanced AC-3 file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public EnhancedAC3Reader(BinaryReader reader) : base(reader) { }

        /// <summary>
        /// Enhanced AC-3 file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public EnhancedAC3Reader(string path) : base(path) { }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            decoder = new EnhancedAC3Decoder(new BlockBuffer<byte>(() => reader.ReadBytes(FormatConsts.blockSize)));
            ChannelCount = decoder.ChannelCount;
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
            if (decoder == null)
                ReadHeader();
            return new EnhancedAC3Renderer(decoder);
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        public override void Seek(long sample) => decoder.Seek(sample);
    }
}