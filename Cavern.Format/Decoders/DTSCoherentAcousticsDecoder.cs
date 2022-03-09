using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a DTS Coherent Acoustics bitstream to raw samples.
    /// </summary>
    internal class DTSCoherentAcousticsDecoder : FrameBasedDecoder {
        /// <summary>
        /// Headers are handled with weird bit size words. This is their sum in bytes.
        /// </summary>
        const int headerLength = 13;

        /// <summary>
        /// Normal frame marker. Sample deficit is not allowed.
        /// </summary>
        const int allowedDeficit = 31;

        /// <summary>
        /// Marks the beginning of a new DCA frame.
        /// </summary>
        const int syncWord = 0x7FFE8001;

        /// <summary>
        /// Possible channel arrangements in DTS Core. The index is the ID read from the file. LFE channel is marked separately.
        /// </summary>
        static readonly ReferenceChannel[][] coreChannelArrangements = {
            new ReferenceChannel[] // 0: mono
                { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 1: dual mono
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 2: stereo
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 3: sum-difference (L+R and L-R)
                { ReferenceChannel.FrontCenter, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 4: left + right total
                { ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 5: 3.x (C, L, R)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 6: 3.x (L, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 7: 4.x (C, L, R, S)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 8: 4.x (L, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 9: 5.x (C, L, R, SL, SR)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 10: 6.x (CL, CR, L, R, SL, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 11: 6.x (C, L, R, RL, RR, GV)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.GodsVoice },
            new ReferenceChannel[] // 12: 6.x (CF, CR, LF, RF, LR, RR)
                { ReferenceChannel.FrontCenter, ReferenceChannel.RearCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearLeft, ReferenceChannel.RearRight },
            new ReferenceChannel[] // 13: 7.x (CL, C, CR, L, R, SL, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 14: 8.x (CL, CR, L, R, SL1, SL2, SR1, SR2)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideLeft,
                    ReferenceChannel.SideRight, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 15: 8.x (CL, C, CR, L, R, SL, S, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.RearCenter, ReferenceChannel.SideRight },
        };

        /// <summary>
        /// Possible sample rates. The index is the ID read from the file.
        /// </summary>
        static readonly ushort[] sampleRates =
            new ushort[16] { 0, 8000, 16000, 32000, 0, 0, 11025, 22050, 44100, 0, 0, 12000, 24000, 48000, 0, 0 };

        /// <summary>
        /// Converts a DTS Coherent Acoustics bitstream to raw samples.
        /// </summary>
        public DTSCoherentAcousticsDecoder(BlockBuffer<byte> reader) : base(reader) { }

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
            ChannelPrototype[] channels = ChannelPrototype.Get(coreChannelArrangements[channelArrangement]);
            if (channels.Length > 5)
                throw new UnsupportedFeatureException(">5.1 core"); // This would require splitting the frames between channels

            ushort sampleRate = sampleRates[extractor.Read(4)];
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