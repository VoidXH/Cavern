using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Format.Transcoders;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    public class EnhancedAC3Decoder : FrameBasedDecoder {
        /// <summary>
        /// The stream is coded in the Enhanced version of AC-3.
        /// </summary>
        public bool Enhanced => header.Decoder == Transcoders.EnhancedAC3.Decoders.EAC3;

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
        public override long Length {
            get {
                if (fileSize != -1)
                    return fileSize * outCache.Length / (frameSize * ChannelCount);
                else
                    throw new RealtimeLengthException();
            }
        }

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
        /// Independently decoded substreams.
        /// </summary>
        readonly Dictionary<int, EnhancedAC3Body> bodies = new Dictionary<int, EnhancedAC3Body>();

        /// <summary>
        /// Rendered samples for each channel.
        /// </summary>
        readonly Dictionary<ReferenceChannel, float[]> outputs = new Dictionary<ReferenceChannel, float[]>();

        /// <summary>
        /// File size to calculate the content length from, assuming AC-3 is constant bitrate.
        /// </summary>
        readonly long fileSize;

        /// <summary>
        /// Bytes per audio frame.
        /// </summary>
        long frameSize;

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
        public EnhancedAC3Decoder(BlockBuffer<byte> reader) : base(reader) => fileSize = -1;

        /// <summary>
        /// Converts an Enhanced AC-3 bitstream to raw samples. When the file size is known, the length can be calculated
        /// from the bitrate assuming AC-3 is constant bitrate.
        /// </summary>
        public EnhancedAC3Decoder(BlockBuffer<byte> reader, long fileSize) : base(reader) => this.fileSize = fileSize;

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

            long frameStart = reader.LastFetchStart;
            do {
                if (Finished) {
                    Array.Clear(outCache, 0, outCache.Length);
                    return outCache;
                }

                EnhancedAC3Body body = bodies[header.SubstreamID];
                body.Update();
                for (int i = 0, c = body.Channels.Count; i < c; ++i)
                    outputs[body.Channels[i]] = body.FrameResult[i];
                if (header.LFE)
                    outputs[ReferenceChannel.ScreenLFE] = body.LFEResult;

                Extensions.Decode(body.GetAuxData());
                ReadHeader();
            } while (header.SubstreamID != 0);

            int outLength = outputs.Count * FrameSize;
            if (outCache.Length != outLength)
                outCache = new float[outputs.Count * FrameSize];

            int channelIndex = 0;
            IOrderedEnumerable<KeyValuePair<ReferenceChannel, float[]>> orderedChannels = outputs.OrderBy(x => x.Key);
            foreach (KeyValuePair<ReferenceChannel, float[]> channel in orderedChannels) {
                WaveformUtils.Insert(channel.Value, outCache, channelIndex++, outputs.Count);
            }

            if (frameStart < reader.LastFetchStart)
                frameSize = reader.LastFetchStart - frameStart;
            return outCache;
        }

        /// <summary>
        /// Reads all metadata for the next frame and prepares audio decoding.
        /// </summary>
        /// <remarks>This decoder has to read the beginning of the next frame to know if it's a beginning.</remarks>
        void ReadHeader() {
            if (reader.Readable) {
                extractor = header.Decode(reader);
                if (extractor == null || !extractor.Readable) {
                    Finished = true;
                    return;
                }
                if (!bodies.ContainsKey(header.SubstreamID)) {
                    bodies[header.SubstreamID] = new EnhancedAC3Body(header);
                }
                bodies[header.SubstreamID].PrepareUpdate(extractor);
            } else
                Finished = true;
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) {
            throw new NotImplementedException();
        }
    }
}