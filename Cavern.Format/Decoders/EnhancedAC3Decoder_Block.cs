using System;

using Cavern.Format.Common;
using Cavern.Format.Decoders.EnhancedAC3;
using static Cavern.Format.InOut.EnhancedAC3;

namespace Cavern.Format.Decoders {
    // Block decoder of E-AC-3
    partial class EnhancedAC3Decoder {
        void AudioBlock(int block) {
            bool eac3 = header.Decoder == InOut.EnhancedAC3.Decoders.EAC3;

            if (blkswe)
                for (int channel = 0; channel < channels.Length; ++channel)
                    blksw[channel] = extractor.ReadBit();
            else
                for (int channel = 0; channel < channels.Length; ++channel)
                    blksw[channel] = false;
            if (dithflage)
                for (int channel = 0; channel < channels.Length; ++channel)
                    dithflag[channel] = extractor.ReadBit();
            else
                for (int channel = 0; channel < channels.Length; ++channel)
                    dithflag[channel] = true;

            dynrng = extractor.ReadConditional(8);
            if (header.ChannelMode == 0)
                dynrng2 = extractor.ReadConditional(8);

            if (eac3)
                ReadSPX(block);
            else
                ClearSPX();

            // (Enhanced) coupling strategy information
            if (!eac3 && (cplstre[block] = extractor.ReadBit()))
                cplinu[block] = extractor.ReadBit();

            if (cplstre[block]) {
                if (cplinu[block]) {
                    ecplinu = eac3 && extractor.ReadBit();
                    if (eac3 && header.ChannelMode == 2)
                        chincpl[0] = chincpl[1] = true;
                    else
                        for (int channel = 0; channel < channels.Length; ++channel)
                            chincpl[channel] = extractor.ReadBit();
                    if (!ecplinu) { // Standard coupling
                        if (header.ChannelMode == 0x2)
                            phsflginu = extractor.ReadBit();
                        cplbegf = extractor.Read(4);
                        if (!spxinu)
                            cplendf = extractor.Read(4);
                        else
                            cplendf = spxbegf < 6 ? spxbegf - 2 : (spxbegf * 2 - 7);
                        ncplsubnd = 3 + cplendf - cplbegf;
                        if (ncplsubnd < 1)
                            throw new DecoderException(3);
                        if (cplbndstrce = !eac3 || extractor.ReadBit())
                            for (int band = 1; band < ncplsubnd; ++band)
                                cplbndstrc[band] = extractor.ReadBit();
                    } else { // Enhanced coupling
                        ecplbegf = extractor.Read(4);
                        if (ecplbegf < 3)
                            ecpl_begin_subbnd = ecplbegf * 2;
                        else if (ecplbegf < 13)
                            ecpl_begin_subbnd = ecplbegf + 2;
                        else
                            ecpl_begin_subbnd = ecplbegf * 2 - 10;
                        if (!spxinu) {
                            ecplendf = extractor.Read(4);
                            ecpl_end_subbnd = ecplendf + 7;
                        } else
                            ecpl_end_subbnd = spxbegf < 6 ? spxbegf + 5 : (spxbegf * 2);
                        if (ecplbndstrce = extractor.ReadBit()) {
                            ecplbndstrc = new bool[ecpl_end_subbnd];
                            for (int sbnd = Math.Max(9, ecpl_begin_subbnd + 1); sbnd < ecpl_end_subbnd; sbnd++)
                                ecplbndstrc[sbnd] = extractor.ReadBit();
                        }
                    }
                } else if (eac3) {
                    for (int channel = 0; channel < channels.Length; ++channel) {
                        chincpl[channel] = false;
                        firstcplcos[channel] = true;
                    }
                    firstcplleak = true;
                    phsflginu = false;
                    ecplinu = false;
                }

                ncplbnd = 0;
                for (int band = 1; band < ncplsubnd; ++band)
                    if (cplbndstrc[band])
                        ++ncplbnd;
                ncplbnd = ncplsubnd - ncplbnd;
            }

            // Coupling coordinates, phase flags
            if (cplinu[block]) {
                if (!ecplinu) { // Standard coupling
                    for (int channel = 0; channel < channels.Length; ++channel) {
                        if (chincpl[channel]) {
                            if (eac3 && firstcplcos[channel]) {
                                cplcoe[channel] = true;
                                firstcplcos[channel] = false;
                            } else
                                cplcoe[channel] = extractor.ReadBit();
                            if (cplcoe[channel]) {
                                mstrcplco[channel] = extractor.Read(2);
                                for (int band = 0; band < ncplbnd; ++band) {
                                    cplcoexp[channel][band] = extractor.Read(4);
                                    cplcomant[channel][band] = extractor.Read(4);
                                }
                            }
                            if ((header.ChannelMode == 0x2) && phsflginu && (cplcoe[0] || cplcoe[1]))
                                throw new UnsupportedFeatureException("stereo");
                        } else
                            firstcplcos[channel] = true;
                    }
                } else { // Enhanced coupling
                    throw new UnsupportedFeatureException("ecplinu");
                }
            }

            if (header.ChannelMode == 2)
                throw new UnsupportedFeatureException("stereo");

            // Exponent strategy
            if (!eac3) {
                if (cplinu[block])
                    cplexpstr[block] = (ExpStrat)extractor.Read(2);
                for (int channel = 0; channel < channels.Length; ++channel)
                    chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
                if (header.LFE)
                    lfeexpstr[block] = extractor.ReadBit();
            }

            // Channel bandwidth code
            for (int channel = 0; channel < channels.Length; ++channel)
                if (chexpstr[block][channel] != ExpStrat.Reuse)
                    if (!chincpl[channel] && !chinspx[channel])
                        chbwcod[channel] = extractor.Read(6);

            // Exponents
            ParseParametricBitAllocation(block);
            if (cplinu[block]) {
                if (cplexpstr[block] != ExpStrat.Reuse) {
                    cplabsexp = extractor.Read(4);
                    cplexps = new int[ncplgrps];
                    for (int group = 0; group < ncplgrps; ++group)
                        cplexps[group] = extractor.Read(7);
                }
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
            if (header.LFE) {
                if (lfeexpstr[block]) {
                    lfeexps[0] = extractor.Read(4);
                    lfeexps[1] = extractor.Read(7);
                    lfeexps[2] = extractor.Read(7);
                }
            }

            // Bit allocation parametric information
            if (bamode) {
                if (baie = extractor.ReadBit()) {
                    sdcycod = extractor.Read(2);
                    fdcycod = extractor.Read(2);
                    sgaincod = extractor.Read(2);
                    dbpbcod = extractor.Read(2);
                    floorcod = extractor.Read(3);
                }
            } else {
                sdcycod = 2;
                fdcycod = 1;
                sgaincod = 1;
                dbpbcod = 2;
                floorcod = 7;
            }

            if (snroffststr == 0) {
                if (cplinu[block])
                    cplfsnroffst = frmfsnroffst;
                for (int channel = 0; channel < channels.Length; ++channel)
                    fsnroffst[channel] = frmfsnroffst;
                if (header.LFE)
                    lfefsnroffst = frmfsnroffst;
            } else {
                if (snroffste = (eac3 && block == 0) || extractor.ReadBit()) {
                    csnroffst = extractor.Read(6);
                    if (!eac3) {
                        throw new UnsupportedFeatureException("ac3");
                    } else if (snroffststr == 1) {
                        blkfsnroffst = extractor.Read(4);
                        cplfsnroffst = blkfsnroffst;
                        for (int channel = 0; channel < channels.Length; ++channel)
                            fsnroffst[channel] = blkfsnroffst;
                        lfefsnroffst = blkfsnroffst;
                    } else if (snroffststr == 2) {
                        if (cplinu[block])
                            cplfsnroffst = extractor.Read(4);
                        for (int channel = 0; channel < channels.Length; ++channel)
                            fsnroffst[channel] = extractor.Read(4);
                        if (header.LFE)
                            lfefsnroffst = extractor.Read(4);
                    }
                }
            }

            if (eac3) {
                if (fgaincode = frmfgaincode && extractor.ReadBit()) {
                    if (cplinu[block])
                        cplfgaincod = extractor.Read(3);
                    for (int channel = 0; channel < channels.Length; ++channel)
                        fgaincod[channel] = extractor.Read(3);
                    if (header.LFE)
                        lfefgaincod = extractor.Read(3);
                } else {
                    if (cplinu[block])
                        cplfgaincod = 4;
                    for (int channel = 0; channel < channels.Length; ++channel)
                        fgaincod[channel] = 4;
                    if (header.LFE)
                        lfefgaincod = 4;
                }

                if (header.StreamType == StreamTypes.Independent && (convsnroffste = extractor.ReadBit()))
                    convsnroffst = extractor.Read(10);
            }

            if (cplinu[block]) {
                bool cplleake;
                if (firstcplleak) {
                    cplleake = true;
                    firstcplleak = false;
                } else
                    cplleake = extractor.ReadBit();
                if (cplleake) {
                    cplfleak = extractor.Read(3);
                    cplsleak = extractor.Read(3);
                }
            }

            // Delta bit allocation
            if ((dbaflde || !eac3) && (deltbaie = extractor.ReadBit())) {
                if (cplinu[block])
                    cpldeltba.enabled = (DeltaBitAllocationMode)extractor.Read(2);
                for (int channel = 0; channel < channels.Length; ++channel)
                    deltba[channel].enabled = (DeltaBitAllocationMode)extractor.Read(2);
                if (cplinu[block] && cpldeltba.enabled == DeltaBitAllocationMode.NewInfoFollows)
                    cpldeltba.Read(extractor);
                for (int channel = 0; channel < channels.Length; ++channel)
                    if (deltba[channel].enabled == DeltaBitAllocationMode.NewInfoFollows)
                        deltba[channel].Read(extractor);
            }

            // Error checks
            if (block == 0) {
                if (!cplstre[block])
                    throw new DecoderException(1);
                if (header.LFE && !lfeexpstr[block])
                    throw new DecoderException(10);
            }
            for (int channel = 0; channel < channels.Length; ++channel) {
                if (block == 0 && chexpstr[0][channel] == ExpStrat.Reuse)
                    throw new DecoderException(8);
                if (!chincpl[channel] && chbwcod[channel] > 60)
                    throw new DecoderException(11);
            }

            // Unused dummy data
            if (skipflde && extractor.ReadBit()) {
                extractor.Skip(extractor.Read(9) * 8); // TODO: merge
            }

            // Quantized mantissa values
            if (cplinu[block] && cplexpstr[block] != ExpStrat.Reuse)
                cplbap = AllocateCoupling(cplexpstr[block]);

            bool got_cplchan = false;
            for (int channel = 0; channel < channels.Length; ++channel) {
                if (chahtinu[channel] == 0) {
                    chmant[channel] = new int[nchmant[channel]];
                    if (chexpstr[block][channel] != ExpStrat.Reuse)
                        bap[channel] = Allocate(channel, exps[channel], chexpstr[block][channel]);
                    if (bap[channel] == null)
                        throw new DecoderException(-1);
                    for (int bin = 0; bin < nchmant[channel]; ++bin)
                        chmant[channel][bin] = extractor.Read(bap[channel][bin]);
                } else
                    throw new UnsupportedFeatureException("AHT");

                if (cplinu[block] && chincpl[channel] && !got_cplchan) {
                    if (cplahtinu == 0) {
                        int ncplmant = 12 * ncplsubnd;
                        cplmant = new int[ncplmant];
                        for (int bin = 0; bin < ncplmant; ++bin)
                            cplmant[bin] = extractor.Read(cplbap[bin]);
                        got_cplchan = true;
                    } else
                        throw new UnsupportedFeatureException("AHT");
                }
            }

            if (header.LFE) {
                if (lfeahtinu == 0) {
                    if (lfeexpstr[block])
                        lfebap = AllocateLFE(lfeexps, ExpStrat.D15);
                    if (lfebap == null)
                        throw new DecoderException(-1);
                    for (int bin = 0; bin < nlfemant; ++bin)
                        lfemant[bin] = extractor.Read(lfebap[bin]);
                } else
                    throw new UnsupportedFeatureException("AHT");
            }
        }
    }
}