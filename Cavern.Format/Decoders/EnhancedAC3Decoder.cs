using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    internal partial class EnhancedAC3Decoder : FrameBasedDecoder {
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
            int wordsPerSyncframe = extractor.Read(11) + 1;
            int frameSize = wordsPerSyncframe * 2;
            int fscod = extractor.Read(2);
            int sampleRate = sampleRates[fscod];
            int numblkscod = extractor.Read(2);
            int blocks = numberOfBlocks[numblkscod];

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

            // ------------------------------------------------------------------
            // Audio frame
            // ------------------------------------------------------------------
            bool expstre = true;
            if (numblkscod == 3) {
                expstre = extractor.ReadBit();
                if (extractor.ReadBit())
                    throw new UnsupportedFeatureException("ahte");
            }

            int snroffststr = extractor.Read(2);
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("transproce");
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("blkswe");
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("dithflage");
            bool bamode = extractor.ReadBit();
            bool frmfgaincode = extractor.ReadBit();
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("dbaflde");
            bool skipflde = extractor.ReadBit();
            if (extractor.ReadBit())
                throw new UnsupportedFeatureException("spxattene");

            bool[] cplstre = new bool[blocks], cplinu = new bool[blocks];
            if (acmod > 1) { // Not mono
                cplstre[0] = true;
                cplinu[0] = extractor.ReadBit();
                for (int block = 1; block < blocks; ++block) {
                    cplstre[block] = extractor.ReadBit();
                    if (cplstre[block])
                        cplinu[block] = extractor.ReadBit();
                    else
                        cplinu[block] = cplinu[block - 1];
                }
            }

            int[][] chexpstr = new int[blocks][];
            int ncplblks = 0;
            for (int block = 0; block < blocks; ++block) {
                chexpstr[block] = new int[channels.Length];
                if (cplinu[block])
                    ++ncplblks;
            }

            int[] cplexpstr = new int[blocks];
            int frmcplexpstr;
            int[] frmchexpstr = new int[channels.Length];
            if (expstre) {
                for (int block = 0; block < blocks; ++block) {
                    if (cplinu[block])
                        cplexpstr[block] = extractor.Read(2);
                    for (int channel = 0; channel < channels.Length; ++channel)
                        chexpstr[block][channel] = extractor.Read(2);
                }
            } else {
                if (acmod > 1 && ncplblks > 0)
                    frmcplexpstr = extractor.Read(5);
                for (int channel = 0; channel < channels.Length; ++channel)
                    frmchexpstr[channel] = extractor.Read(5);
            }

            bool[] lfeexpstr = new bool[blocks];
            for (int block = 0; block < blocks; ++block)
                lfeexpstr[block] = extractor.ReadBit();

            int[] convexpstr = new int[channels.Length];
            if (streamType == 0) {
                bool convexpstre = true;
                if (numblkscod != 3)
                    convexpstre = extractor.ReadBit();
                if (convexpstre)
                    for (int channel = 0; channel < channels.Length; ++channel)
                        convexpstr[channel] = extractor.Read(5);
            }

            int frmcsnroffst, frmfsnroffst;
            if (snroffststr == 0) {
                frmcsnroffst = extractor.Read(6);
                frmfsnroffst = extractor.Read(4);
            }

            bool blockStartInfoEnabled = false;
            if (numblkscod != 0)
                blockStartInfoEnabled = extractor.ReadBit();
            if (blockStartInfoEnabled) {
                int blockStartInfoBits = (blocks - 1) * (4 + QMath.Log2Ceil(wordsPerSyncframe));
                extractor.Skip(blockStartInfoBits); // Block start info (omitted)
            }

            bool[] firstspxcos = new bool[channels.Length], firstcplcos = new bool[channels.Length];
            for (int channel = 0; channel < channels.Length; ++channel) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            bool firstcplleak = true;

            float[] result = new float[blocks * 256];
            for (int block = 0; block < blocks; ++block) {
                // TODO: audioblk
            }
            WaveformUtils.Gain(result, QMath.DbToGain(programScaleFactor));

            // TODO: auxdata
            // TODO: errorcheck
            throw new NotImplementedException();
        }
    }
}