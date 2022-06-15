using System;
using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Format.Transcoders;
using Cavern.Format.Utilities;
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    public class EnhancedAC3Decoder : FrameBasedDecoder {
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
        /// Auxillary data bytes which might contain object-based extensions.
        /// </summary>
        readonly BitExtractor aux = new BitExtractor();

        /// <summary>
        /// Header data container and reader.
        /// </summary>
        readonly EnhancedAC3Header header = new EnhancedAC3Header();

        /// <summary>
        /// Independently decoded substreams.
        /// </summary>
        readonly Dictionary<int, EnhancedAC3Body> bodies = new Dictionary<int, EnhancedAC3Body>();

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
            if (outputs.Count == 0)
                ReadHeader();

            do {
                if (Finished) {
                    Array.Clear(outCache, 0, outCache.Length);
                    return outCache;
                }

                EnhancedAC3Body body = bodies[header.SubstreamID];
                aux.Clear(); // TODO: fill with skipfields
                body.Update();
                for (int i = 0, end = body.Channels.Count; i < end; ++i)
                    outputs[body.Channels[i]] = body.FrameResult[i];
                if (header.LFE)
                    outputs[ReferenceChannel.ScreenLFE] = body.LFEResult;

                Extensions.Decode(body.GetAuxData());
                ReadHeader();
            } while (header.SubstreamID != 0);

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
                if (!bodies.ContainsKey(header.SubstreamID))
                    bodies[header.SubstreamID] = new EnhancedAC3Body(header);
                bodies[header.SubstreamID].PrepareUpdate(extractor);
            } else
                Finished = true;
        }
    }
}