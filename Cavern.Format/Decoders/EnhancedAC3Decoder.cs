using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples.
    /// </summary>
    internal partial class EnhancedAC3Decoder : FrameBasedDecoder {
        /// <summary>
        /// Content sample rate.
        /// </summary>
        public int SampleRate { get; private set; }

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

            BitstreamInfo(ref extractor);
            AudioFrame(extractor);

            // ------------------------------------------------------------------
            // Audio blocks
            // ------------------------------------------------------------------
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
                        spxstrtf = extractor.Read(2);
                        spxbegf = extractor.Read(3);
                        spxendf = extractor.Read(3);
                        spx_begin_subbnd = spxbegf < 6 ? spxbegf + 2 : (spxbegf * 2 - 3);
                        spx_end_subbnd = spxendf < 3 ? spxendf + 5 : (spxendf * 2 + 3);
                        spxbndstrce = extractor.ReadBit();
                        if (spxbndstrce) {
                            spxbndstrc = new bool[spx_end_subbnd];
                            for (int band = spx_begin_subbnd + 1; band < spx_end_subbnd; ++band)
                                spxbndstrc[band] = extractor.ReadBit();
                        }

                        // Processing
                        nspxbnds = 1;
                        spxbndsztab = new int[spx_end_subbnd];
                        spxbndsztab[0] = 12;
                        for (int bnd = spx_begin_subbnd + 1; bnd < spx_end_subbnd; ++bnd) {
                            if (!spxbndstrc[bnd]) {
                                spxbndsztab[nspxbnds] = 12;
                                ++nspxbnds;
                            } else
                                spxbndsztab[nspxbnds - 1] += 12;
                        }
                    } else {
                        for (int channel = 0; channel < channels.Length; ++channel) {
                            chinspx[channel] = false;
                            firstspxcos[channel] = true;
                        }
                    }
                }

                // Spectral extension strategy coordinates
                if (spxinu) {
                    for (int channel = 0; channel < channels.Length; ++channel) {
                        if (chinspx[channel]) {
                            if (firstspxcos[channel]) {
                                spxcoe[channel] = true;
                                firstspxcos[channel] = false;
                            } else
                                spxcoe[channel] = extractor.ReadBit();

                            if (spxcoe[channel]) {
                                spxblnd[channel] = extractor.Read(5);
                                mstrspxco[channel] = extractor.Read(2);
                                for (int band = 0; band < nspxbnds; ++band) {
                                    spxcoexp[channel][band] = extractor.Read(4);
                                    spxcomant[channel][band] = extractor.Read(2);
                                }
                            }
                        } else
                            firstspxcos[channel] = true;
                    }
                }

                // (Enhanced) coupling strategy information
                if (cplstre[block]) {
                    if (cplinu[block]) {
                        ecplinu = extractor.ReadBit();
                        if (acmod == 2)
                            chincpl[0] = chincpl[1] = true;
                        else
                            for (int channel = 0; channel < channels.Length; ++channel)
                                chincpl[channel] = extractor.ReadBit();
                        if (!ecplinu) { // Standard coupling
                            if (acmod == 0x2)
                                phsflginu = extractor.ReadBit();
                            cplbegf = extractor.Read(4);
                            if (!spxinu)
                                cplendf = extractor.Read(4);
                            else
                                cplendf = spxbegf < 6 ? spxbegf - 2 : (spxbegf * 2 - 7);
                            if (extractor.ReadBit()) {
                                int ncplsubnd = 3 + cplendf - cplbegf;
                                cplbndstrc = new bool[ncplsubnd];
                                for (int band = 1; band < ncplsubnd; ++band)
                                    cplbndstrc[band] = extractor.ReadBit();
                            }
                        } else { // Enhanced coupling
                            ecplbegf = extractor.Read(4);
                            if (ecplbegf < 3)
                                ecpl_begin_subbnd = ecplbegf * 2;
                            else if (ecplbegf < 13)
                                ecpl_begin_subbnd = ecplbegf + 2;
                            else
                                ecpl_begin_subbnd = ecplbegf * 2 - 10;
                            if (!spxinu)
                                ecplendf = extractor.Read(4);
                            else
                                ecpl_end_subbnd = spxbegf < 6 ? spxbegf + 5 : (spxbegf * 2);
                            if (extractor.ReadBit()) {
                                ecplbndstrc = new bool[ecpl_end_subbnd];
                                for (int sbnd = Math.Max(9, ecpl_begin_subbnd + 1); sbnd < ecpl_end_subbnd; sbnd++)
                                    ecplbndstrc[sbnd] = extractor.ReadBit();
                            }
                        }
                    } else {
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
                if (cplinu[block]) {
                    if (!ecplinu) { // Standard coupling
                        for (int channel = 0; channel < channels.Length; ++channel) {
                            // TODO
                        }
                    } else { // Enhanced coupling
                        throw new UnsupportedFeatureException("ecplinu");
                    }
                }

                // Channel bandwidth code
                for (int channel = 0; channel < channels.Length; ++channel)
                    if (chexpstr[block][channel] != ExpStrat.Reuse)
                        if (!chincpl[channel] && !chinspx[channel])
                            chbwcod[channel] = extractor.Read(6);

                // Parametric bit allocation
                for (int channel = 0; channel < channels.Length; ++channel) {
                    int endmant;
                    if (ecplinu) {
                        endmant = ecplsubbndtab[ecpl_begin_subbnd];
                    } else {
                        if (spxinu && !cplinu[block])
                            //endmant = spxbandtable[spx_begin_subbnd];
                            throw new UnsupportedFeatureException("spxbandtable");
                        else if (cplinu[block])
                            endmant = cplbegf * 12 + 37;
                        else
                            endmant = (chbwcod[channel] + 12) * 3 + 37;
                    }
                    nchmant[channel] = endmant;
                    nchgrps[channel] = endmant;
                    if (chexpstr[block][channel] != ExpStrat.Reuse)
                        nchgrps[channel] /= 3 << ((int)chexpstr[block][channel] - 1);
                }

                // Exponents for full bandwidth channels
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (chexpstr[block][channel] != ExpStrat.Reuse) {
                        int expl = nchgrps[channel] + 1;
                        if (exps[channel] == null || exps[channel].Length != expl)
                            exps[channel] = new int[expl];
                        exps[channel][0] = extractor.Read(4);
                        for (int group = 1; group <= nchgrps[channel]; ++group)
                            exps[channel][group] = extractor.Read(7);
                        gainrng[channel] = extractor.Read(2);
                    }
                }

                // Exponents for LFE channel
                if (lfeon) {
                    if (lfeexpstr[block]) {
                        lfeexps[0] = extractor.Read(4);
                        lfeexps[1] = extractor.Read(7);
                        lfeexps[2] = extractor.Read(7);
                    }
                }

                // Bit allocation parametric information
                BitAllocation bitAllocInfo = new BitAllocation(extractor, block, channels.Length, lfeon,
                    bamode, snroffststr, frmfsnroffst, frmfgaincode, fscod);

                // Error checks
                if (block == 0) {
                    if (!cplstre[block])
                        throw new DecoderException(1);
                    if (lfeon && !lfeexpstr[block])
                        throw new DecoderException(10);
                    if (!bitAllocInfo.snroffste)
                        throw new DecoderException(13);
                }
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (block == 0 && chexpstr[0][channel] == ExpStrat.Reuse)
                        throw new DecoderException(8);
                    if (!chincpl[channel] && chbwcod[channel] > 60)
                        throw new DecoderException(11);
                }

                if (strmtyp == 0)
                    if (extractor.ReadBit())
                        convsnroffst = extractor.Read(10);

                // Unused dummy data
                if (skipflde && extractor.ReadBit())
                    extractor.Skip(extractor.Read(9) * 8);

                // Quantized mantissa values
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (chahtinu[channel] == 0) {
                        chmant[channel] = new int[nchmant[channel]];
                        int[] bap = bitAllocInfo.Allocate(nchmant, channel, nchgrps[channel],
                            exps[channel], chexpstr[block][channel]);
                        for (int bin = 0; bin < nchmant[channel]; ++bin)
                            chmant[channel][bin] = extractor.Read(bap[bin]);
                    } else
                        throw new UnsupportedFeatureException("chahtinu");

                    if (cplinu[block])
                        throw new UnsupportedFeatureException("cplinu");
                }

                if (lfeon) {
                    // AHT not handled
                    int[] bap = bitAllocInfo.AllocateLFE(lfeexps, // TODO: LFE parts
                        lfeexpstr[block] ? ExpStrat.D15 : ExpStrat.Reuse);
                    for (int bin = 0; bin < nlfemant; ++bin)
                        lfemant[bin] = extractor.Read(bap[bin]);
                }
            }

            // TODO: auxdata
            // TODO: errorcheck
            throw new NotImplementedException();
        }
    }
}