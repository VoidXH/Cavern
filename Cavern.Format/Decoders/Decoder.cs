using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a bitstream to raw samples.
    /// </summary>
    internal abstract class Decoder {
        /// <summary>
        /// Stream reader and block regrouping object.
        /// </summary>
        protected readonly BlockBuffer<byte> reader;

        /// <summary>
        /// Converts a bitstream to raw samples.
        /// </summary>
        public Decoder(BlockBuffer<byte> reader) => this.reader = reader;

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public abstract void DecodeBlock(float[] target, long from, long to);
    }
}