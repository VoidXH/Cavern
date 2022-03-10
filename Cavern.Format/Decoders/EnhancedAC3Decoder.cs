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
            Decoder decoder = ParseDecoder(extractor.Read(5));
            float dialogNormalization = ParseDialogNormalization(extractor.Read(5));
            if (extractor.ReadBit()) // compr, omitted
                extractor.Skip(8);

            // Mixing and mapping metadata
            int dmixmod, ltrtcmixlev, lorocmixlev, ltrtsurmixlev, lorosurmixlev, lfemixlevcod,
                programScaleFactor = 0, // Gain offset for the entire stream in dB.
                extpgmscl;
            int[] blkmixcfginfo = new int[blocks];
            if (extractor.ReadBit()) {
                if (acmod > 2)
                    dmixmod = extractor.Read(2);
                if (((acmod & 1) != 0) && (acmod > 2)) { // 3 front channels present
                    ltrtcmixlev = extractor.Read(3);
                    lorocmixlev = extractor.Read(3);
                }
                if ((acmod & 0x4) != 0) { // Surround present
                    ltrtsurmixlev = extractor.Read(3);
                    lorosurmixlev = extractor.Read(3);
                }
                if (LFE) // LFE present
                    lfemixlevcod = extractor.ReadBit() ? extractor.Read(5) : 0;
                if (streamType == 0) { // Independent stream
                    programScaleFactor = extractor.ReadBit() ? extractor.Read(6) - 51 : 0;
                    extpgmscl = extractor.ReadBit() ? extractor.Read(6) : 0;
                    if (extractor.Read(2) != 0)
                        throw new UnsupportedFeatureException("mixing options");
                }
                if (extractor.ReadBit())
                    throw new UnsupportedFeatureException("mixing config");
                if (extractor.ReadBit()) { // Mixing configuration information
                    if (numblkscod == 0)
                        blkmixcfginfo[0] = extractor.Read(5);
                    else
                        for (int block = 0; block < blocks; ++block)
                            blkmixcfginfo[block] = extractor.ReadBit() ? extractor.Read(5) : 0;
                }
            }

            if (extractor.ReadBit()) { // Informational metadata
                if (extractor.Read(3) != 0)
                    throw new UnsupportedFeatureException("bit stream modes");
                extractor.Skip(2); // Copyright & original bitstream bits
                if (acmod >= 6 && extractor.Read(2) != 0)
                    throw new UnsupportedFeatureException("ProLogic");
                if (extractor.ReadBit())
                    throw new UnsupportedFeatureException("audio production info");
                if (fscod < 3)
                    extractor.Skip(1); // The sample rate was halved from the original
            }

            if ((streamType == 0) && (numblkscod != 3))
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
            bool transproce = extractor.ReadBit();
            bool blkswe = extractor.ReadBit();
            bool dithflage = extractor.ReadBit();
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

            // Exponent strategy data init
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
            if (LFE)
                for (int block = 0; block < blocks; ++block)
                    lfeexpstr[block] = extractor.ReadBit();

            // Converter exponent strategy data
            int[] convexpstr = new int[channels.Length];
            if (streamType == 0) {
                if (numblkscod == 3 || extractor.ReadBit())
                    for (int channel = 0; channel < channels.Length; ++channel)
                        convexpstr[channel] = extractor.Read(5);
            }

            // Audio frame SNR offset data
            int frmcsnroffst, frmfsnroffst = 0;
            if (snroffststr == 0) {
                frmcsnroffst = extractor.Read(6);
                frmfsnroffst = extractor.Read(4);
            }

            // Transient pre-noise processing data
            if (transproce)
                for (int channel = 0; channel < channels.Length; ++channel)
                    if (extractor.ReadBit()) // chintransproc
                        extractor.Skip(18); // transprocloc and transproclen

            if (numblkscod != 0 && extractor.ReadBit()) {
                int blockStartInfoBits = (blocks - 1) * (4 + QMath.Log2Ceil(wordsPerSyncframe));
                extractor.Skip(blockStartInfoBits); // Block start info (omitted)
            }

            // Syntax state init
            bool[] firstspxcos = new bool[channels.Length], firstcplcos = new bool[channels.Length];
            for (int channel = 0; channel < channels.Length; ++channel) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            bool firstcplleak = true;

            // ------------------------------------------------------------------
            // Audio blocks
            // ------------------------------------------------------------------
            bool spxinu = false,
                phsflginu = false,
                ecplinu = false;
            bool[] blksw = new bool[channels.Length],
                dithflag = new bool[channels.Length],
                chinspx = new bool[channels.Length],
                chincpl = new bool[channels.Length];
            int convsnroffst;
            int[] chbwcod = new int[channels.Length],
                lfeexps = new int[nlfegrps + 1],
                chahtinu = new int[channels.Length],
                lfemant = new int[nlfemant];
            int[][] exps = new int[channels.Length][],
                chmant = new int[channels.Length][];
            float[] result = new float[blocks * 256];
            for (int block = 0; block < blocks; ++block) {
                if (blkswe)
                    for (int channel = 0; channel < channels.Length; ++channel)
                        blksw[channel] = extractor.ReadBit();
                if (dithflage)
                    for (int channel = 0; channel < channels.Length; ++channel)
                        dithflag[channel] = extractor.ReadBit();
                else
                    for (int channel = 0; channel < channels.Length; ++channel)
                        dithflag[channel] = true;

                if (extractor.ReadBit()) // dynrng, omitted
                    extractor.Skip(8);

                // Spectral extension strategy information
                if (block == 0 || extractor.ReadBit()) {
                    spxinu = extractor.ReadBit();
                    if (spxinu) {
                        if (acmod == 1)
                            chinspx[0] = true;
                        else
                            for (int channel = 0; channel < channels.Length; ++channel)
                                    chinspx[channel] = extractor.ReadBit();
                        int spxstrtf = extractor.Read(2);
                        int spxbegf = extractor.Read(3);
                        int spxendf = extractor.Read(3);
                        int spx_begin_subbnd = spxbegf < 6 ? spxbegf + 2 : (spxbegf * 2 - 3);
                        int spx_end_subbnd = spxendf < 3 ? spxendf + 5 : (spxendf * 2 + 3);
                        bool spxbndstrce = extractor.ReadBit();
                        if (spxbndstrce) {
                            bool[] spxbndstrc = new bool[spx_end_subbnd];
                            for (int band = spx_begin_subbnd + 1; band < spx_end_subbnd; ++band)
                                spxbndstrc[band] = extractor.ReadBit();
                        }
                    } else {
                        for (int channel = 0; channel < channels.Length; ++channel) {
                            chinspx[channel] = false;
                            firstspxcos[channel] = true;
                        }
                    }
                }

                // Spectral extension strategy coordinates
                if (spxinu)
                    throw new UnsupportedFeatureException("spxinu"); // TODO

                // (Enhanced) coupling strategy information
                if (cplstre[block]) {
                    if (cplinu[block])
                        throw new UnsupportedFeatureException("cplinu");
                    else {
                        for (int channel = 0; channel < channels.Length; ++channel) {
                            chincpl[channel] = false;
                            firstcplcos[channel] = true;
                        }
                        firstcplleak = true;
                        phsflginu = false;
                        ecplinu = false;
                    }
                }

                // Coupling coordinates
                if (cplinu[block])
                    throw new UnsupportedFeatureException("cplinu");

                // Channel bandwidth code
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (chexpstr[block][channel] != (int)ExponentStrategies.Reuse) {
                        if (!chincpl[channel] && !chinspx[channel]) {
                            chbwcod[channel] = extractor.Read(6);
                        }
                    }
                }

                // Parametric bit allocation
                int[] nchgrps = new int[channels.Length],
                    nchmant = new int[channels.Length]; // = endmant
                for (int channel = 0; channel < channels.Length; ++channel) {
                    int endmant;
                    if (cplinu[channel])
                        throw new UnsupportedFeatureException("cplinu");
                    else
                        endmant = (chbwcod[channel] + 12) * 3 + 37;
                    nchmant[channel] = endmant;

                    nchgrps[channel] = endmant;
                    if (chexpstr[block][channel] != 0)
                        nchgrps[channel] /= 3 << (chexpstr[block][channel] - 1);
                }

                // Exponents for full bandwidth channels
                int[] gainrng = new int[channels.Length];
                for (int channel = 0; channel < channels.Length; ++channel) {
                    int expl = nchgrps[channel] + 1;
                    if (exps[channel] == null || exps[channel].Length != expl)
                        exps[channel] = new int[expl];
                    if (chexpstr[block][channel] != (int)ExponentStrategies.Reuse) {
                        exps[channel][0] = extractor.Read(4);
                        for (int group = 1; group <= nchgrps[channel]; ++group)
                            exps[channel][group] = extractor.Read(7);
                        gainrng[channel] = extractor.Read(2);
                    }
                }

                // Exponents for LFE channel
                if (LFE) {
                    if (lfeexpstr[block]) {
                        lfeexps[0] = extractor.Read(4);
                        lfeexps[1] = extractor.Read(7);
                        lfeexps[2] = extractor.Read(7);
                    }
                }

                // Bit allocation parametric information
                BitAllocation bitAllocInfo = new BitAllocation(extractor, block, channels.Length, LFE,
                    bamode, snroffststr, frmfsnroffst, frmfgaincode, fscod);

                if (streamType == 0) {
                    if (extractor.ReadBit())
                        convsnroffst = extractor.Read(10);
                }

                // Unused dummy data
                if (skipflde && extractor.ReadBit())
                    extractor.Skip(extractor.Read(9) * 8);

                // Quantized mantissa values
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (chahtinu[channel] == 0) {
                        chmant[channel] = new int[nchmant[channel]];
                        int[] bap = bitAllocInfo.Allocate(nchmant, channel, nchgrps[channel],
                            exps[channel], (ExponentStrategies)chexpstr[block][channel]);
                        for (int bin = 0; bin < nchmant[channel]; ++bin)
                            chmant[channel][bin] = extractor.Read(bap[bin]);
                    } else
                        throw new UnsupportedFeatureException("chahtinu");

                    if (cplinu[block])
                        throw new UnsupportedFeatureException("cplinu");
                }

                if (LFE) {
                    // AHT not handled
                    int[] bap = bitAllocInfo.AllocateLFE(lfeexps,
                        lfeexpstr[block] ? ExponentStrategies.D15 : ExponentStrategies.Reuse);
                    for (int bin = 0; bin < nlfemant; ++bin)
                        lfemant[bin] = extractor.Read(bap[bin]);
                }

                // Error checks
                if (block == 0) {
                    if (!cplstre[block])
                        throw new DecoderException(1);
                    if (LFE && !lfeexpstr[block])
                        throw new DecoderException(10);
                    if (!bitAllocInfo.snroffste)
                        throw new DecoderException(13);
                }
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (block == 0 && chexpstr[0][channel] == 0)
                        throw new DecoderException(8);
                    if (!chincpl[channel] && chbwcod[channel] > 60)
                        throw new DecoderException(11);
                }
            }
            WaveformUtils.Gain(result, QMath.DbToGain(programScaleFactor));

            // TODO: auxdata
            // TODO: errorcheck
            throw new NotImplementedException();
        }
    }
}