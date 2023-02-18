using Cavern.Format.Common;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Decoder for a format that can't be decoded.
    /// </summary>
    public class DummyDecoder : Decoder {
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
            Position = -1;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Mark the unsupported codec on decoding.
        /// </summary>
        public override void DecodeBlock(float[] target, long from, long to) =>
            throw new UnsupportedCodecException(true, format);

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) => throw new UnsupportedCodecException(true, format);
    }
}