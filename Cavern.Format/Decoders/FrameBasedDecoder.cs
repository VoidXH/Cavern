using System;

using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a frame-based bitstream to raw samples.
    /// Only a single function is required which processes a single frame, with access to the <see cref="Decoder.reader"/>.
    /// </summary>
    public abstract class FrameBasedDecoder : Decoder {
        /// <summary>
        /// Location in the stream in samples. Unsupported for this codec.
        /// </summary>
        public override long Position => -1;

        /// <summary>
        /// The position of the first sample of the last exported block in the buffer.
        /// </summary>
        public int LastFetchStart => decoder.LastFetchStart;

        /// <summary>
        /// Frame cache object.
        /// </summary>
        readonly BlockBuffer<float> decoder;

        /// <summary>
        /// Converts a frame-based bitstream to raw samples.
        /// </summary>
        protected FrameBasedDecoder(BlockBuffer<byte> reader) : base(reader) =>
            decoder = new BlockBuffer<float>(DecodeFrame);

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void DecodeBlock(float[] target, long from, long to) {
            const long skip = FormatConsts.blockSize / sizeof(float); // source split optimization for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip) {
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                }
                return;
            }

            float[] source = decoder.Read((int)(to - from));
            Array.Copy(source, 0, target, from, source.LongLength);
        }

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected abstract float[] DecodeFrame();
    }
}
