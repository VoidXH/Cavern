using System;
using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Format.InOut;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using static Cavern.Format.InOut.EnhancedAC3;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    public partial class EnhancedAC3Decoder : FrameBasedDecoder {
        /// <summary>
        /// True if the stream has reached its end.
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// Number of total output channels.
        /// </summary>
        public override int ChannelCount => outputs.Count;

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public override long Length => throw new RealtimeLengthException();

        /// <summary>
        /// Content sample rate.
        /// </summary>
        public override int SampleRate => header.SampleRate;

        /// <summary>
        /// Samples in each decoded frame
        /// </summary>
        public int FrameSize => header.Blocks * 256;

        /// <summary>
        /// Header data container and reader.
        /// </summary>
        readonly EnhancedAC3Header header = new EnhancedAC3Header();

        /// <summary>
        /// Rendered samples for each channel.
        /// </summary>
        readonly Dictionary<ReferenceChannel, float[]> outputs = new Dictionary<ReferenceChannel, float[]>();

        /// <summary>
        /// Auxillary metadata parsed for the last decoded frame.
        /// </summary>
        internal ExtensibleMetadataDecoder Extensions { get; private set; } = new ExtensibleMetadataDecoder();

        /// <summary>
        /// Reads through the current frame.
        /// </summary>
        BitExtractor extractor;

        /// <summary>
        /// Reusable output sample array.
        /// </summary>
        float[] outCache = new float[0];

        /// <summary>
        /// Converts an Enhanced AC-3 bitstream to raw samples.
        /// </summary>
        public EnhancedAC3Decoder(BlockBuffer<byte> reader) : base(reader) { }

        /// <summary>
        /// Get the bed channels.
        /// </summary>
        public ReferenceChannel[] GetChannels() {
            ReferenceChannel[] result = new ReferenceChannel[outputs.Count];
            outputs.Keys.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected override float[] DecodeFrame() {
            if (channels == null)
                ReadHeader();

            do {
                if (Finished) {
                    Array.Clear(outCache, 0, outCache.Length);
                    return outCache;
                }

                // Create or reuse per-channel outputs
                for (int i = 0; i < channels.Length; ++i) {
                    if (outputs.ContainsKey(channels[i]) && outputs[channels[i]].Length == FrameSize)
                        Array.Clear(outputs[channels[i]], 0, FrameSize);
                    else
                        outputs[channels[i]] = new float[FrameSize];
                }
                if (header.LFE) {
                    if (outputs.ContainsKey(ReferenceChannel.ScreenLFE) &&
                        outputs[ReferenceChannel.ScreenLFE].Length == FrameSize)
                        Array.Clear(outputs[ReferenceChannel.ScreenLFE], 0, FrameSize);
                    else
                        outputs[ReferenceChannel.ScreenLFE] = new float[FrameSize];
                }

                for (int block = 0; block < cplstre.Length; ++block)
                    AudioBlock(block);

                Extensions.Decode(extractor);
                ReadHeader();
            } while (header.StreamType == StreamTypes.Dependent);

            int outLength = outputs.Count * FrameSize;
            if (outCache.Length != outLength)
                outCache = new float[outputs.Count * FrameSize];
            // TODO: interlace channels by a standard matrix
            return outCache;
        }

        /// <summary>
        /// Reads all metadata for the next frame and prepares audio decoding.
        /// </summary>
        /// <remarks>This decoder has to read the beginning of the next frame to know if it's a beginning.</remarks>
        void ReadHeader() {
            if (reader.Readable) {
                extractor = header.Decode(reader);
                channels = header.GetChannelArrangement();
                CreateCacheTables(header.Blocks, channels.Length);
                if (header.Decoder == InOut.EnhancedAC3.Decoders.EAC3)
                    AudioFrame();
                else {
                    blkswe = true;
                    dithflage = true;
                    bamode = true;
                    snroffststr = -1;
                    firstcplleak = false;
                    // TODO: disable AHT when it's implemented
                }

                // Clear per-frame reusable data
                Array.Clear(bap, 0, bap.Length);
                lfebap = null;
            } else
                Finished = true;
        }
    }
}