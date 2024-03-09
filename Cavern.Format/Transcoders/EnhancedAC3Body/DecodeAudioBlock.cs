using System;

using Cavern.Format.Common;

using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Decode an (E-)AC-3 audio block.
        /// </summary>
        /// <param name="block">Number of the block in the currently decoded syncframe</param>
        internal void DecodeAudioBlock(int block) {
            bool eac3 = header.Decoder == EnhancedAC3.Decoders.EAC3;

            if (blkswe) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    blksw[channel] = extractor.ReadBit();
                }
            } else {
                for (int channel = 0; channel < channels.Length; channel++) {
                    blksw[channel] = false;
                }
            }
            if (dithflage) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    dithflag[channel] = extractor.ReadBit();
                }
            } else {
                for (int channel = 0; channel < channels.Length; channel++) {
                    dithflag[channel] = true;
                }
            }

            dynrng = extractor.ReadConditional(8);
            if (header.ChannelMode == 0) {
                dynrng2 = extractor.ReadConditional(8);
            }

            if (eac3) {
                ReadSPX(block);
            } else {
                spxinu = false;
                ClearSPX();
            }

            DecodeCouplingStrategy(eac3, block);
            if (cplinu[block]) {
                DecodeCouplingCoordinates(eac3);
            }

            if (header.ChannelMode == 2) {
                throw new UnsupportedFeatureException("stereo");
            }

            // Exponent strategy
            if (!eac3) {
                if (cplinu[block]) {
                    cplexpstr[block] = (ExpStrat)extractor.Read(2);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
                }
                if (header.LFE) {
                    lfeexpstr[block] = extractor.ReadBit();
                }
            }

            // Channel bandwidth code
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chexpstr[block][channel] != ExpStrat.Reuse && !chincpl[channel] && !chinspx[channel]) {
                    chbwcod[channel] = extractor.Read(6);
                }
            }

            // Exponents
            ParseParametricBitAllocation(block);
            if (cplinu[block] && cplexpstr[block] != ExpStrat.Reuse) {
                couplingAllocation.ReadCouplingExponents(extractor, cplexpstr[block], cplstrtmant, ncplgrps);
            }

            // Exponents for full bandwidth channels
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chexpstr[block][channel] != ExpStrat.Reuse) {
                    allocation[channel].ReadChannelExponents(extractor, chexpstr[block][channel], nchgrps[channel]);
                }
            }

            // Exponents for LFE channel
            if (header.LFE && lfeexpstr[block]) {
                lfeAllocation.ReadLFEExponents(extractor);
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
                csnroffst = frmcsnroffst;
                if (cplinu[block]) {
                    cplfsnroffst = frmfsnroffst;
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    fsnroffst[channel] = frmfsnroffst;
                }
                if (header.LFE) {
                    lfefsnroffst = frmfsnroffst;
                }
            } else {
                if (snroffste = (eac3 && block == 0) || extractor.ReadBit()) {
                    csnroffst = extractor.Read(6);
                    if (!eac3) {
                        if (cplinu[block]) {
                            cplfsnroffst = extractor.Read(4);
                            cplfgaincod = extractor.Read(3);
                        }
                        for (int channel = 0; channel < channels.Length; channel++) {
                            fsnroffst[channel] = extractor.Read(4);
                            fgaincod[channel] = extractor.Read(3);
                        }
                        if (header.LFE) {
                            lfefsnroffst = extractor.Read(4);
                            lfefgaincod = extractor.Read(3);
                        }
                    } else if (snroffststr == 1) {
                        blkfsnroffst = extractor.Read(4);
                        cplfsnroffst = blkfsnroffst;
                        for (int channel = 0; channel < channels.Length; channel++) {
                            fsnroffst[channel] = blkfsnroffst;
                        }
                        lfefsnroffst = blkfsnroffst;
                    } else if (snroffststr == 2) {
                        if (cplinu[block]) {
                            cplfsnroffst = extractor.Read(4);
                        }
                        for (int channel = 0; channel < channels.Length; channel++) {
                            fsnroffst[channel] = extractor.Read(4);
                        }
                        if (header.LFE) {
                            lfefsnroffst = extractor.Read(4);
                        }
                    }
                }
            }

            if (eac3) {
                if (fgaincode = frmfgaincode && extractor.ReadBit()) {
                    if (cplinu[block]) {
                        cplfgaincod = extractor.Read(3);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        fgaincod[channel] = extractor.Read(3);
                    }
                    if (header.LFE) {
                        lfefgaincod = extractor.Read(3);
                    }
                } else {
                    if (cplinu[block]) {
                        cplfgaincod = 4;
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        fgaincod[channel] = 4;
                    }
                    if (header.LFE) {
                        lfefgaincod = 4;
                    }
                }

                if (header.StreamType == StreamTypes.Independent && (convsnroffste = extractor.ReadBit())) {
                    convsnroffst = extractor.Read(10);
                }
            }

            if (cplinu[block]) {
                if (firstcplleak) {
                    cplleake = true;
                    firstcplleak = false;
                } else {
                    cplleake = extractor.ReadBit();
                }
                if (cplleake) {
                    cplfleak = extractor.Read(3);
                    cplsleak = extractor.Read(3);
                }
            }

            // Delta bit allocation
            if ((dbaflde || !eac3) && (deltbaie = extractor.ReadBit())) {
                if (cplinu[block]) {
                    cpldeltba.enabled = (DeltaBitAllocationMode)extractor.Read(2);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    deltba[channel].enabled = (DeltaBitAllocationMode)extractor.Read(2);
                }
                if (cplinu[block] && cpldeltba.enabled == DeltaBitAllocationMode.NewInfoFollows) {
                    cpldeltba.Read(extractor);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (deltba[channel].enabled == DeltaBitAllocationMode.NewInfoFollows) {
                        deltba[channel].Read(extractor);
                    }
                }
            }

            // Error checks
            if (block == 0) {
                if (!cplstre[block]) {
                    throw new DecoderException(1);
                }
                if (header.LFE && !lfeexpstr[block]) {
                    throw new DecoderException(10);
                }
            }
            for (int channel = 0; channel < channels.Length; channel++) {
                if (block == 0 && chexpstr[0][channel] == ExpStrat.Reuse) {
                    throw new DecoderException(8);
                }
                if (!chincpl[channel] && chbwcod[channel] > 60) {
                    throw new DecoderException(11);
                }
            }

            // "Unused dummy data" that might just be used to transport objects
            if (skipFieldSyntaxEnabled && (skipLengthEnabled = extractor.ReadBit())) {
                extractor.ReadBytesInto(ref auxData, ref auxDataPos, skipLength = extractor.Read(9));
            }

            // Quantized mantissa values - prepare for the next allocation frame
            bap1Pos = 2;
            bap2Pos = 2;
            bap4Pos = 1;

            if (cplinu[block]) {
                AllocateCoupling();
            }

            bool got_cplchan = false;
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chahtinu[channel] == 0) {
                    Allocate(channel);
                    allocation[channel].ReadTransformCoeffs(extractor, channelOutput[channel], 0, endmant[channel]);
                } else {
                    throw new UnsupportedFeatureException("AHT");
                }

                if (cplinu[block] && chincpl[channel] && !got_cplchan) {
                    if (cplahtinu == 0) {
                        couplingAllocation.ReadTransformCoeffs(extractor, couplingTransformCoeffs, cplstrtmant, cplendmant);
                        got_cplchan = true;
                    } else {
                        throw new UnsupportedFeatureException("AHT");
                    }
                }
            }

            // Output
            for (int channel = 0; channel < channels.Length; channel++) {
                if (cplinu[block] && chincpl[channel]) {
                    ApplyCoupling(channel);
                }
                if (blksw[channel]) {
                    allocation[channel].IMDCT256(channelOutput[channel]);
                } else {
                    allocation[channel].IMDCT512(channelOutput[channel]);
                }
                Array.Copy(channelOutput[channel], 0, FrameResult[channel], block * 256, 256);
            }

            // Combined mantissa and output handling for LFE
            if (header.LFE) {
                if (lfeahtinu == 0) {
                    AllocateLFE();
                    lfeAllocation.ReadTransformCoeffs(extractor, lfeOutput, lfestrtmant, lfeendmant);
                    lfeAllocation.IMDCT512(lfeOutput);
                    Array.Copy(lfeOutput, 0, LFEResult, block * 256, 256);
                } else {
                    throw new UnsupportedFeatureException("AHT");
                }
            }
        }
    }
}