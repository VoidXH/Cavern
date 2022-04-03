using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Format.Utilities;
using Cavern.Remapping;

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
        /// Samples in each decoded frame
        /// </summary>
        public int FrameSize => blocks * 256;

        /// <summary>
        /// Auxillary metadata parsed for the last decoded frame.
        /// </summary>
        internal ExtensibleMetadataDecoder Extensions { get; private set; } = new ExtensibleMetadataDecoder();

        /// <summary>
        /// Reads through the current frame.
        /// </summary>
        BitExtractor extractor;

        /// <summary>
        /// Rendered samples for each channel.
        /// </summary>
        Dictionary<ReferenceChannel, float[]> outputs = new Dictionary<ReferenceChannel, float[]>();

        /// <summary>
        /// Reusable output sample array.
        /// </summary>
        float[] outCache = new float[0];

        /// <summary>
        /// Converts an Enhanced AC-3 bitstream to raw samples.
        /// </summary>
        public EnhancedAC3Decoder(BlockBuffer<byte> reader) : base(reader) { }

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected override float[] DecodeFrame() {
            Dictionary<ReferenceChannel, float[]> lastOut = outputs;
            outputs = new Dictionary<ReferenceChannel, float[]>();
            if (Channels == null)
                ReadHeader();

            do {
                // Create or reuse per-channel outputs
                for (int i = 0; i < Channels.Length; ++i) {
                    if (lastOut.ContainsKey(Channels[i]))
                        outputs[Channels[i]] = lastOut[Channels[i]];
                    else
                        outputs[Channels[i]] = new float[FrameSize];
                }
                if (lfeon) {
                    if (lastOut.ContainsKey(ReferenceChannel.ScreenLFE))
                        outputs[ReferenceChannel.ScreenLFE] = lastOut[ReferenceChannel.ScreenLFE];
                    else
                        outputs[ReferenceChannel.ScreenLFE] = new float[FrameSize];
                }

                // TODO: decode actual audio data
                //for (int block = 0; block < blocks; ++block)
                //    AudioBlock(block);

                Extensions.Decode(extractor);
                ReadHeader();
            } while (streamType == StreamTypes.Dependent);

            int outLength = outputs.Count * FrameSize;
            if (outCache.Length != outLength)
                outCache = new float[outputs.Count * FrameSize];
            // TODO: interlace channels by a standard matrix
            return outCache;
        }

        /// <summary>
        /// Reads all metadata for the next frame.
        /// </summary>
        /// <remarks>This decoder has to read the beginning of the next frame to know if it's a beginning.</remarks>
        void ReadHeader() {
            extractor = new BitExtractor(reader.Read(mustDecode));
            if (extractor.Read(16) != syncWord)
                throw new SyncException();

            BitstreamInfo();
            AudioFrame();
        }
    }
}