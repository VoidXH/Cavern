using System;

using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    partial class EnhancedAC3Decoder {
        /// <summary>
        /// Bitstream data extraction from the frame header.
        /// </summary>
        void BitstreamInfo() {
            streamType = (StreamTypes)extractor.Read(2);
            substreamid = extractor.Read(3);
            words_per_syncframe = extractor.Read(11) + 1;
            fscod = extractor.Read(2);
            numblkscod = extractor.Read(2);
            acmod = extractor.Read(3);
            lfeon = extractor.ReadBit();
            decoder = ParseDecoder(extractor.Read(5));

            if (decoder == Decoders.EAC3) {
                blocks = numberOfBlocks[numblkscod];
                extractor.Expand(reader.Read(words_per_syncframe * 2 - mustDecode));
            } else {
                streamType = StreamTypes.Repackaged;
                blocks = 6;
                fscod = extractor[4] >> 6;
                int frameSize = frameSizes[(extractor[4] & 0b00111110) >> 1];
                if (fscod == 1) { // 44.1 kHz
                    frameSize = frameSize / 2 * 1393 / 1280;
                    if ((extractor[4] & 1) == 1)
                        ++frameSize;
                    frameSize *= 2;
                } else if (fscod == 2)
                    frameSize = frameSize * 3 / 2;
                extractor.Expand(reader.Read(frameSize - mustDecode));
                bsmod = extractor.Read(3);
                acmod = extractor.Read(3);
            }

            if (streamType == StreamTypes.Reserved)
                throw new ReservedValueException("strmtyp");
            if (fscod == 3)
                throw new ReservedValueException("fscod");
            SampleRate = sampleRates[fscod];
            Channels = channelArrangements[acmod];
            CreateCacheTables(blocks, Channels.Length);

            switch (decoder) {
                case Decoders.AlternateAC3:
                    HeaderAlternativeAC3();
                    break;
                case Decoders.AC3:
                    throw new UnsupportedFeatureException("legacy");
                case Decoders.EAC3:
                    HeaderEAC3();
                    break;
            }
        }

        void HeaderAlternativeAC3() {
            if ((acmod & 0x1) != 0 && (acmod != 0x1)) // 3 fronts exist
                cmixlev = extractor.Read(2);
            if ((acmod & 0x4) != 0) // Surrounds exist
                surmixlev = extractor.Read(2);
            if (acmod == 0x2) // Stereo
                dsurmod = extractor.Read(2);
            lfeon = extractor.ReadBit();
            dialnorm = extractor.Read(5);
            if (compre = extractor.ReadBit())
                compr = extractor.Read(8);
            // TODO
        }

        void HeaderEAC3() {
            dialnorm = extractor.Read(5);
            if (compre = extractor.ReadBit())
                compr = extractor.Read(8);

            if (acmod == 0) {
                dialnorm2 = extractor.Read(5);
                if (compr2e = extractor.ReadBit())
                    compr2 = extractor.Read(8);
            }

            if (streamType == StreamTypes.Dependent && extractor.ReadBit())
                ParseChannelMap(extractor.ReadBits(16));
            if (mixmdate = extractor.ReadBit())
                MixingMetadata();
            if (infomdate = extractor.ReadBit())
                InfoMetadata();
            if (streamType == StreamTypes.Independent && numblkscod != 3)
                convsync = extractor.ReadBit();
            if (streamType == StreamTypes.Repackaged && (blkid = numblkscod == 3 || extractor.ReadBit()))
                extractor.Skip(6); // AC-3 frame size code

            if (addbsie = extractor.ReadBit()) // Additional bit stream information (omitted)
                extractor.Skip((extractor.Read(6) + 1) * 8);
        }

        /// <summary>
        /// Parse mixing and mapping metadata.
        /// </summary>
        void MixingMetadata() {
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
            if (lfeon && (lfemixlevcode = extractor.ReadBit())) // LFE present
                lfemixlevcod = extractor.Read(5);
            if (streamType == StreamTypes.Independent) {
                if (pgmscle = extractor.ReadBit())
                    pgmscl = extractor.Read(6);
                if (acmod == 0 && (pgmscl2e = extractor.ReadBit()))
                    pgmscl2 = extractor.Read(6);
                if (extpgmscle = extractor.ReadBit())
                    extpgmscl = extractor.Read(6);
                mixdef = extractor.Read(2);
                if (mixdef != 0)
                    throw new UnsupportedFeatureException("mixdef");
                if (acmod < 2)
                    throw new UnsupportedFeatureException("mono");
                if (frmmixcfginfoe = extractor.ReadBit()) { // Mixing configuration information
                    if (numblkscod == 0)
                        blkmixcfginfo[0] = extractor.Read(5);
                    else
                        for (int block = 0; block < blocks; ++block)
                            blkmixcfginfo[block] = extractor.ReadBit() ? extractor.Read(5) : 0;
                }
            }
        }

        /// <summary>
        /// Parse informational metadata.
        /// </summary>
        void InfoMetadata() {
            bsmod = extractor.Read(3);
            extractor.Skip(2); // Copyright & original bitstream bits
            if (acmod == 2) {
                dsurmod = extractor.Read(2);
                dheadphonmod = extractor.Read(2);
            }
            if (acmod >= 6)
                dsurexmod = extractor.Read(2);
            if (audprodie = extractor.ReadBit()) {
                mixlevel = extractor.Read(5);
                roomtyp = extractor.Read(2);
                adconvtyp = extractor.ReadBit();
            }
            if (acmod == 0) {
                if (audprodie2 = extractor.ReadBit()) {
                    mixlevel2 = extractor.Read(5);
                    roomtyp2 = extractor.Read(2);
                    adconvtyp2 = extractor.ReadBit();
                }
            }
            if (fscod < 3)
                sourcefscod = extractor.ReadBit();
        }

        void AudioFrame() {
            expstre = true;
            if (numblkscod == 3) {
                expstre = extractor.ReadBit();
                ahte = extractor.ReadBit();
            }

            snroffststr = extractor.Read(2);
            transproce = extractor.ReadBit();
            blkswe = extractor.ReadBit();
            dithflage = extractor.ReadBit();
            bamode = extractor.ReadBit();
            frmfgaincode = extractor.ReadBit();
            dbaflde = extractor.ReadBit();
            skipflde = extractor.ReadBit();
            spxattene = extractor.ReadBit();

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
            if (expstre) {
                for (int block = 0; block < blocks; ++block) {
                    if (cplinu[block])
                        cplexpstr[block] = (ExpStrat)extractor.Read(2);
                    for (int channel = 0; channel < Channels.Length; ++channel)
                        chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
                }
            } else {
                int ncplblks = 0;
                for (int block = 0; block < blocks; ++block)
                    if (cplinu[block])
                        ++ncplblks;
                if (acmod > 1 && ncplblks > 0)
                    frmcplexpstr = extractor.Read(5);
                for (int channel = 0; channel < Channels.Length; ++channel)
                    frmchexpstr[channel] = extractor.Read(5);

                for (int block = 0; block < blocks; ++block) {
                    cplexpstr[block] = frmcplexpstr_tbl[frmcplexpstr][block];
                    for (int channel = 0; channel < Channels.Length; ++channel)
                        chexpstr[block][channel] = frmcplexpstr_tbl[frmchexpstr[channel]][block];
                }
            }

            if (lfeon)
                for (int block = 0; block < blocks; ++block)
                    lfeexpstr[block] = extractor.ReadBit();

            // Converter exponent strategy data
            if (streamType == StreamTypes.Independent && (convexpstre = numblkscod == 3 || extractor.ReadBit()))
                for (int channel = 0; channel < Channels.Length; ++channel)
                    convexpstr[channel] = extractor.Read(5);

            // AHT data
            if (ahte)
                throw new UnsupportedFeatureException("AHT");

            // Audio frame SNR offset data
            if (snroffststr == 0) {
                frmcsnroffst = extractor.Read(6);
                frmfsnroffst = extractor.Read(4);
            }

            // Transient pre-noise processing data
            if (transproce) {
                for (int channel = 0; channel < Channels.Length; ++channel) {
                    if (chintransproc[channel] = extractor.ReadBit()) {
                        transprocloc[channel] = extractor.Read(10);
                        transproclen[channel] = extractor.Read(8);
                    }
                }
            }

            // Spectral extension attenuation data
            if (spxattene)
                for (int ch = 0; ch < Channels.Length; ++ch)
                    if (chinspxatten[ch] = extractor.ReadBit())
                        spxattencod[ch] = extractor.Read(5);

            blkstrtinfoe = numblkscod != 0 && extractor.ReadBit();
            if (blkstrtinfoe) {
                int nblkstrtbits = (blocks - 1) * (4 + QMath.Log2Ceil(words_per_syncframe));
                blkstrtinfo = extractor.Read((byte)nblkstrtbits);
            }

            // Syntax state init
            for (int channel = 0; channel < Channels.Length; ++channel) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            firstcplleak = true;

            // Clear per-frame reuse data
            Array.Clear(bap, 0, bap.Length);
            lfebap = null;
        }

        void AudioBlock(int block) {
            if (blkswe)
                for (int channel = 0; channel < Channels.Length; ++channel)
                    blksw[channel] = extractor.ReadBit();
            if (dithflage)
                for (int channel = 0; channel < Channels.Length; ++channel)
                    dithflag[channel] = extractor.ReadBit();
            else
                for (int channel = 0; channel < Channels.Length; ++channel)
                    dithflag[channel] = true;

            if (dynrnge = extractor.ReadBit())
                dynrng = extractor.Read(8);
            if (acmod == 0 && (dynrng2e = extractor.ReadBit()))
                dynrng2 = extractor.Read(8);

            // Spectral extension strategy information
            if (spxstre = block == 0 || extractor.ReadBit()) {
                if (spxinu = extractor.ReadBit()) {
                    if (acmod == 1)
                        chinspx[0] = true;
                    else
                        for (int channel = 0; channel < Channels.Length; ++channel)
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

                    ParseSPX();
                } else {
                    for (int channel = 0; channel < Channels.Length; ++channel) {
                        chinspx[channel] = false;
                        firstspxcos[channel] = true;
                    }
                }
            }

            // Spectral extension strategy coordinates
            if (spxinu) {
                for (int channel = 0; channel < Channels.Length; ++channel) {
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
                        for (int channel = 0; channel < Channels.Length; ++channel)
                            chincpl[channel] = extractor.ReadBit();
                    if (!ecplinu) { // Standard coupling
                        if (acmod == 0x2)
                            phsflginu = extractor.ReadBit();
                        cplbegf = extractor.Read(4);
                        if (!spxinu)
                            cplendf = extractor.Read(4);
                        else
                            cplendf = spxbegf < 6 ? spxbegf - 2 : (spxbegf * 2 - 7);
                        if (cplbndstrce = extractor.ReadBit()) {
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
                } else {
                    for (int channel = 0; channel < Channels.Length; ++channel) {
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
                    throw new UnsupportedFeatureException("cplinu");
                } else { // Enhanced coupling
                    throw new UnsupportedFeatureException("ecplinu");
                }
            }

            if (acmod == 2)
                throw new UnsupportedFeatureException("stereo");

            // Channel bandwidth code
            for (int channel = 0; channel < Channels.Length; ++channel)
                if (chexpstr[block][channel] != ExpStrat.Reuse)
                    if (!chincpl[channel] && !chinspx[channel])
                        chbwcod[channel] = extractor.Read(6);

            // Exponents    
            if (cplinu[block])
                throw new UnsupportedFeatureException("cplinu");

            ParseParametricBitAllocation(block);

            // Exponents for full bandwidth channels
            for (int channel = 0; channel < Channels.Length; ++channel) {
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
                for (int channel = 0; channel < Channels.Length; ++channel)
                    fsnroffst[channel] = frmfsnroffst;
                if (lfeon)
                    lfefsnroffst = frmfsnroffst;
            } else {
                if (snroffste = block == 0 || extractor.ReadBit()) {
                    csnroffst = extractor.Read(6);
                    if (snroffststr == 1) {
                        blkfsnroffst = extractor.Read(4);
                        cplfsnroffst = blkfsnroffst;
                        for (int channel = 0; channel < Channels.Length; ++channel)
                            fsnroffst[channel] = blkfsnroffst;
                        lfefsnroffst = blkfsnroffst;
                    } else if (snroffststr == 2) {
                        if (cplinu[block])
                            cplfsnroffst = extractor.Read(4);
                        for (int channel = 0; channel < Channels.Length; ++channel)
                            fsnroffst[channel] = extractor.Read(4);
                        if (lfeon)
                            lfefsnroffst = extractor.Read(4);
                    }
                }
            }

            if (fgaincode = frmfgaincode && extractor.ReadBit()) {
                if (cplinu[block])
                    cplfgaincod = extractor.Read(3);
                for (int channel = 0; channel < Channels.Length; ++channel)
                    fgaincod[channel] = extractor.Read(3);
                if (lfeon)
                    lfefgaincod = extractor.Read(3);
            } else {
                if (cplinu[block])
                    cplfgaincod = 4;
                for (int channel = 0; channel < Channels.Length; ++channel)
                    fgaincod[channel] = 4;
                if (lfeon)
                    lfefgaincod = 4;
            }

            if (streamType == StreamTypes.Independent && (convsnroffste = extractor.ReadBit()))
                convsnroffst = extractor.Read(10);

            if (cplinu[block])
                throw new UnsupportedFeatureException("cplinu");

            // Delta bit allocation
            if (dbaflde)
                throw new UnsupportedFeatureException("dbaflde");

            // Error checks
            if (block == 0) {
                if (!cplstre[block])
                    throw new DecoderException(1);
                if (lfeon && !lfeexpstr[block])
                    throw new DecoderException(10);
            }
            for (int channel = 0; channel < Channels.Length; ++channel) {
                if (block == 0 && chexpstr[0][channel] == ExpStrat.Reuse)
                    throw new DecoderException(8);
                if (!chincpl[channel] && chbwcod[channel] > 60)
                    throw new DecoderException(11);
            }

            // Unused dummy data
            if (skipflde && extractor.ReadBit())
                extractor.Skip(extractor.Read(9) * 8);

            // Quantized mantissa values
            for (int channel = 0; channel < Channels.Length; ++channel) {
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

                if (cplinu[block])
                    throw new UnsupportedFeatureException("cplinu");
            }

            if (lfeon) {
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