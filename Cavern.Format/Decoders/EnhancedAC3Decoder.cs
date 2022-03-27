using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    public partial class EnhancedAC3Decoder : FrameBasedDecoder {
        /// <summary>
        /// Content sample rate.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Auxillary metadata parsed for the last decoded frame.
        /// </summary>
        internal ExtensibleMetadataDecoder Extensions { get; private set; }

        /// <summary>
        /// Converts an Enhanced AC-3 bitstream to raw samples.
        /// </summary>
        public EnhancedAC3Decoder(BlockBuffer<byte> reader) : base(reader) { }

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected override float[] DecodeFrame() {
            BitExtractor extractor = new BitExtractor(reader.Read(mustDecode));
            if (extractor.Read(16) != syncWord)
                throw new SyncException();

            BitstreamInfo(ref extractor);
            AudioFrame(extractor);

            float[] result = new float[blocks * 256 * ChannelCount];
            // TODO: decode actual audio data
            //for (int block = 0; block < blocks; ++block)
            //    AudioBlock(extractor, block);

            Extensions = new ExtensibleMetadataDecoder(extractor);
            return result;
        }
    }
}