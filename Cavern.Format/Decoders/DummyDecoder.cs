using Cavern.Format.Common;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Decoder for a format that can't be decoded.
    /// </summary>
    public class DummyDecoder : Decoder {
        /// <summary>
        /// Content channel count.
        /// </summary>
        public override int ChannelCount { get; }

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public override long Length { get; }

        /// <summary>
        /// Bitstream sample rate.
        /// </summary>
        public override int SampleRate { get; }

        /// <summary>
        /// Unsupported codec type.
        /// </summary>
        readonly Codec format;

        /// <summary>
        /// Decoder for a format that can't be decoded.
        /// </summary>
        public DummyDecoder(Codec format, int channelCount, long length, int sampleRate) {
            this.format = format;
            ChannelCount = channelCount;
            Length = length;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Mark the unsupported codec on decoding.
        /// </summary>
        public override void DecodeBlock(float[] target, long from, long to) =>
            throw new UnsupportedCodecException(true, format);
    }
}