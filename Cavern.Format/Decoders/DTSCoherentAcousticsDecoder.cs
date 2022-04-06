using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a DTS Coherent Acoustics bitstream to raw samples.
    /// </summary>
    internal partial class DTSCoherentAcousticsDecoder : FrameBasedDecoder {
        /// <summary>
        /// Converts a DTS Coherent Acoustics bitstream to raw samples.
        /// </summary>
        public DTSCoherentAcousticsDecoder(BlockBuffer<byte> reader) : base(reader) { }

        /// <summary>
        /// Content channel count.
        /// </summary>
        public override int ChannelCount => channels.Length + 1;

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public override long Length => throw new RealtimeLengthException();

        /// <summary>
        /// Bitstream sample rate.
        /// </summary>
        public override int SampleRate => sampleRate;
        int sampleRate;

        /// <summary>
        /// Main channel layout, without LFE.
        /// </summary>
        ChannelPrototype[] channels;

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected override float[] DecodeFrame() {
            // ------------------------------------------------------------------
            // Frame header
            // ------------------------------------------------------------------
            BitExtractor extractor = new BitExtractor(reader.Read(headerLength));
            int sync = extractor.Read(32); // TODO: handle the DTS-HD MA sync word, extract objects when documented
            if (sync != syncWord)
                throw new SyncException();

            extractor.Skip(1); // True if the frame is not a terminator, which can be used for video sync

            int deficitSampleCount = extractor.Read(5); // Samples missing from a terminator block
            if (deficitSampleCount != allowedDeficit)
                throw new UnsupportedFeatureException("terminator frames");

            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("CRC");

            int pcmBlockCount = extractor.Read(7) + 1; // A PCM block contains 32 samples for each channel
            if (pcmBlockCount < 5)
                throw new ArgumentOutOfRangeException("PCM block count");

            int frameSize = extractor.Read(14) + 1; // Total size of the current frame including audio and extensions
            if (frameSize < 95)
                throw new ArgumentOutOfRangeException("frame size");

            int channelArrangement = extractor.Read(6);
            if (channelArrangement >= coreChannelArrangements.Length)
                throw new ArgumentOutOfRangeException("channel layout");
            channels = ChannelPrototype.Get(coreChannelArrangements[channelArrangement]);
            if (channels.Length > 5)
                throw new UnsupportedFeatureException(">5.1 core"); // This would require splitting the frames between channels

            sampleRate = sampleRates[extractor.Read(4)];
            if (sampleRate == 0)
                throw new ArgumentOutOfRangeException("sample rate");

            bool lossless = extractor.Read(5) == 29; // These 5 bits mark the bitrate

            if (extractor.ReadBit())
                throw new ArgumentOutOfRangeException("reserved bit");

            bool embeddedDynamicRange = extractor.ReadBit(); // Marks if DRC coefficients are present in a frame
            if (embeddedDynamicRange)
                throw new UnsupportedFeatureException("DRC");

            bool timestamps = extractor.ReadBit(); // Marks if timestamps are embedded after audio frames
            bool auxPresent = extractor.ReadBit(); // Marks if auxillary data is found after audio frames
            extractor.Skip(1); // Marks if the source was mastered in HDCD
            extractor.Skip(3); // Extended audio descriptor flag, none of those are supported
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("extended audio");

            // If false, sync word is placed after each subframe, otherwise subsubframe
            bool syncWordPlacement = extractor.ReadBit();

            int lfeInterploationFactor = extractor.Read(2); // If 0, LFE is disabled
            if (lfeInterploationFactor != 0)
                lfeInterploationFactor = 192 - lfeInterploationFactor * 64;

            bool usePredictorHistory = extractor.ReadBit(); // Don't discard the predictor history from last frame
            bool perfectReconstruction = extractor.ReadBit(); // Uses a different set of multirate interpolation filter coeffs

            int encoderVersion = extractor.Read(4);
            if (encoderVersion > 7)
                throw new UnsupportedFeatureException("encoder");

            extractor.Skip(2); // Audio copy history, omitted deliberately

            int sourceResolution = extractor.Read(3);
            if (sourceResolution % 2 == 1)
                throw new UnsupportedFeatureException("DTS-ES");
            int bitDepth = (sourceResolution >> 1) * 4 + 16;
            if (bitDepth == 28)
                bitDepth = 24;

            bool sumDifferenceFront = extractor.ReadBit();
            bool sumDifferenceSurround = extractor.ReadBit();
            if (sumDifferenceFront || sumDifferenceSurround)
                throw new UnsupportedFeatureException("sum-difference");

            extractor.Skip(4); // Dialog compression - skipped as Cavern is against the loudness war

            // ------------------------------------------------------------------
            // Primary coding header
            // ------------------------------------------------------------------
            extractor = new BitExtractor(reader.Read(frameSize - headerLength)); // also includes data
            int substreamCount = extractor.Read(4) + 1;
            int primaryChannelCount = extractor.Read(3) + 1;

            throw new NotImplementedException(); // TODO: continue core decoding
        }
    }
}