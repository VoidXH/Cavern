using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    internal class EnhancedAC3Decoder : FrameBasedDecoder {
        /// <summary>
        /// First word of all AC-3 frames.
        /// </summary>
        const int syncWord = 0x0B77;

        /// <summary>
        /// Bytes that must be read before determining the frame size.
        /// </summary>
        const int mustDecode = 5;

        /// <summary>
        /// Number of blocks per sync frame for each code.
        /// </summary>
        readonly static byte[] numberOfBlocks = new byte[] { 1, 2, 3, 6 };

        /// <summary>
        /// Sample rates for each sample rate code.
        /// </summary>
        readonly static ushort[] sampleRates = new ushort[] { 48000, 44100, 32000 };

        /// <summary>
        /// Possible channel arrangements in E-AC3. The index is the ID read from the file. LFE channel is marked separately.
        /// </summary>
        static readonly ReferenceChannel[][] channelArrangements = {
            new ReferenceChannel[] // 0: dual mono
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 1: mono
                { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 2: stereo
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 3: 3.x (L, C, R)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 4: 3.x (L, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 5: 4.x (L, C, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 6: 4.x (L, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 9: 5.x (L, C, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight }
        };

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

            // ------------------------------------------------------------------
            // Bit stream information
            // ------------------------------------------------------------------
            int streamType = extractor.Read(2);
            if (streamType == 1)
                throw new UnsupportedFeatureException("dependent stream");
            if (streamType == 2)
                throw new UnsupportedFeatureException("wrapping");

            int substreamid = extractor.Read(3);
            int frameSize = (extractor.Read(11) + 1) * 2;
            int fscod = extractor.Read(2);
            int sampleRate = sampleRates[fscod];
            int numblkscod = extractor.Read(2);

            int acmod = extractor.Read(3);
            ChannelPrototype[] channels = ChannelPrototype.Get(channelArrangements[acmod]);
            if (channels.Length <= 2)
                throw new UnsupportedFeatureException("not surround");

            bool LFE = extractor.ReadBit();
            extractor = new BitExtractor(reader.Read(frameSize - mustDecode));
            int bsid = extractor.Read(5);
            int dialnorm = extractor.Read(5);
            int compr = extractor.ReadBit() ? extractor.Read(8) : 0;

            // Mixing and mapping metadata
            int dmixmod, ltrtcmixlev, lorocmixlev, ltrtsurmixlev, lorosurmixlev, lfemixlevcod,
                programScaleFactor = 0, // Gain offset for the entire stream in dB.
                extpgmscl;
            if (extractor.ReadBit()) {
                if (acmod > 2) {
                    dmixmod = extractor.Read(2);
                }
                if (((acmod & 1) != 0) && (acmod > 2)) { // 3 front channels present
                    ltrtcmixlev = extractor.Read(3);
                    lorocmixlev = extractor.Read(3);
                }
                if ((acmod & 0x4) != 0) { // Surround present
                    ltrtsurmixlev = extractor.Read(3);
                    lorosurmixlev = extractor.Read(3);
                }
                if (LFE) { // LFE present
                    lfemixlevcod = extractor.ReadBit() ? extractor.Read(5) : 0;
                }
                if (streamType == 0) { // Independent stream
                    programScaleFactor = extractor.ReadBit() ? extractor.Read(6) - 51 : 0;
                    extpgmscl = extractor.ReadBit() ? extractor.Read(6) : 0;
                    if (extractor.Read(2) != 0)
                        throw new UnsupportedFeatureException("mixing options");
                }
                if (extractor.ReadBit())
                    throw new UnsupportedFeatureException("mixing config");
            }

            if (extractor.ReadBit()) { // Informational metadata
                if (extractor.Read(3) != 0)
                    throw new UnsupportedFeatureException("bit stream modes");
                extractor.Skip(1); // Copyright bit
                extractor.Skip(1); // Original bitstream bit
                if (acmod >= 6 && extractor.Read(2) != 0)
                    throw new UnsupportedFeatureException("ProLogic");
                if (extractor.ReadBit())
                    throw new UnsupportedFeatureException("audio production info");
                if (fscod < 3)
                    extractor.Skip(1); // The sample rate was halved from the original
            }

            if ((streamType == 0x0) && (numblkscod != 0x3))
                extractor.ReadBit(); // Converter snychronization flag

            if (extractor.ReadBit()) { // Additional bit stream information (omitted)
                int absiLength = extractor.Read(6);
                extractor.Skip((absiLength + 1) * 8);
            }

            // TODO: audfrm

            float[] result = new float[numberOfBlocks[numblkscod] * 256];
            for (int block = 0; block < numberOfBlocks[numblkscod]; ++block) {
                // TODO: audioblk
            }
            WaveformUtils.Gain(result, QMath.DbToGain(programScaleFactor));

            // TODO: auxdata
            // TODO: errorcheck
            throw new NotImplementedException();
        }
    }
}