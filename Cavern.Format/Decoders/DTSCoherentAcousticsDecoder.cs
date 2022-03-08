using Cavern.Format.Common;
using Cavern.Format.Utilities;
using System;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a DTS Coherent Acoustics bitstream to raw samples.
    /// </summary>
    internal class DTSCoherentAcousticsDecoder : Decoder {
        /// <summary>
        /// Headers are handled with weird bit size words. This is their sum in bytes.
        /// </summary>
        const int headerLength = 15;

        /// <summary>
        /// Marks the beginning of a new DCA frame.
        /// </summary>
        const int syncWord = 0x7FFE8001;

        /// <summary>
        /// Frame cache object.
        /// </summary>
        readonly BlockBuffer<float> decoder;

        /// <summary>
        /// Converts a DTS Coherent Acoustics bitstream to raw samples.
        /// </summary>
        public DTSCoherentAcousticsDecoder(BlockBuffer<byte> reader) : base(reader) =>
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
            const long skip = 10 * 1024 * 1024 / sizeof(float); // 10 MB source splits at max to optimize for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip)
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                return;
            }

            float[] source = decoder.Read((int)(to - from));
            Array.Copy(source, 0, target, from, source.LongLength);
        }

        /// <summary>
        /// Decode a new frame if the cached samples are gone.
        /// </summary>
        float[] DecodeFrame() {
            BitExtractor header = new BitExtractor(reader.Read(headerLength));
            int sync = header.Read(32);
            if (sync != syncWord)
                throw new SyncException();
            return new float[1000]; // TODO: continue in place of this
        }
    }
}