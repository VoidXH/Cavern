using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a DTS Coherent Acoustics bitstream to raw samples.
    /// </summary>
    internal class DTSCoherentAcousticsDecoder : Decoder {
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
        /// Frame cache object.
        /// </summary>
        readonly BlockBuffer<float> decoder;

        /// <summary>
        /// Converts a DTS Coherent Acoustics bitstream to raw samples.
        /// </summary>
        public DTSCoherentAcousticsDecoder(BlockBuffer<byte> reader) : base(reader) =>
            decoder = new BlockBuffer<float>(DecodeFrame);

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void DecodeBlock(float[] target, long from, long to) {
            const long skip = 10 * 1024 * 1024 / sizeof(float); // 10 MB source splits at max to optimize for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip)
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                return;
            }

            float[] source = decoder.Read((int)(to - from));
            Array.Copy(source, 0, target, from, source.LongLength);
        }

        /// <summary>
        /// Decode a new frame if the cached samples are gone.
        /// </summary>
        float[] DecodeFrame() {
            // ------------------------------------------------------------------
            // Frame header
            // ------------------------------------------------------------------
            BitExtractor header = new BitExtractor(reader.Read(headerLength));
            int sync = header.Read(32);
            if (sync != syncWord)
                throw new SyncException();

            header.ReadBit(); // True if the frame is not a terminator, which can be used for video sync
            int deficitSampleCount = header.Read(5); // Samples missing from a terminator block
            if (deficitSampleCount != allowedDeficit)
                throw new UnsupportedFeatureException("terminator frames");

            bool crcPresent = header.ReadBit();
            if (crcPresent)
                throw new UnsupportedFeatureException("CRC");

            int pcmBlockCount = header.Read(7) + 1; // A PCM block contains 32 samples for each channel
            if (pcmBlockCount < 5)
                throw new ArgumentOutOfRangeException("PCM block count");

            int frameSize = header.Read(14); // Total size of the current frame including audio and extensions
            if (frameSize < 95)
                throw new ArgumentOutOfRangeException("frame size");

            int channelArrangement = header.Read(6);
            if (channelArrangement >= coreChannelArrangements.Length)
                throw new ArgumentOutOfRangeException("channel layout");
            ChannelPrototype[] channels = ChannelPrototype.Get(coreChannelArrangements[channelArrangement]);

            ushort sampleRate = sampleRates[header.Read(4)];
            if (sampleRate == 0)
                throw new ArgumentOutOfRangeException("sample rate");

            bool lossless = header.Read(5) == 29; // These 5 bits mark the bitrate

            if (header.ReadBit())
                throw new ArgumentOutOfRangeException("reserved bit");

            bool embeddedDynamicRange = header.ReadBit(); // Marks if DRC coefficients are present in a frame
            if (embeddedDynamicRange)
                throw new UnsupportedFeatureException("DRC");

            bool timestamps = header.ReadBit(); // Marks if timestamps are embedded after audio frames
            bool auxPresent = header.ReadBit(); // Marks if auxillary data is found after audio frames
            header.ReadBit(); // Marks if the source was mastered in HDCD
            header.Read(3); // Extended audio descriptor flag, none of those are supported
            if (header.ReadBit())
                throw new UnsupportedFeatureException("extended audio");

            bool syncWordPlacement = header.ReadBit(); // If false, sync word is placed after each subframe, otherwise subsubframe

            int lfeInterploationFactor = header.Read(2); // If 0, LFE is disabled
            if (lfeInterploationFactor != 0)
                lfeInterploationFactor = 192 - lfeInterploationFactor * 64;

            bool usePredictorHistory = header.ReadBit(); // Don't discard the predictor history from last frame
            bool perfectReconstruction = header.ReadBit(); // Uses a different set of multirate interpolation filter coefficients

            int encoderVersion = header.Read(4);
            if (encoderVersion > 7)
                throw new UnsupportedFeatureException("encoder");

            header.Read(2); // Audio copy history, omitted deliberately

            int sourceResolution = header.Read(3);
            if (sourceResolution % 2 == 1)
                throw new UnsupportedFeatureException("DTS-ES");
            int bitDepth = (sourceResolution >> 1) * 4 + 16;
            if (bitDepth == 28)
                bitDepth = 24;

            bool sumDifferenceFront = header.ReadBit();
            bool sumDifferenceSurround = header.ReadBit();
            if (sumDifferenceFront || sumDifferenceSurround)
                throw new UnsupportedFeatureException("sum-difference");

            header.Read(4); // Dialog compression - skipped as Cavern is against the loudness war

            // ------------------------------------------------------------------
            // Primary coding header
            // ------------------------------------------------------------------
            return new float[1000]; // TODO: continue in place of this
        }
    }
}