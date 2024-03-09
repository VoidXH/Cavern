using Cavern.Format.Common;
using Cavern.Format.Utilities;

using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Encode an (E-)AC-3 audio block.
        /// </summary>
        /// <param name="planter">Bitstream to write to</param>
        /// <param name="block">Number of the block in the currently decoded syncframe</param>
        internal void EncodeAudioBlock(BitPlanter planter, int block) {
            bool eac3 = header.Decoder == EnhancedAC3.Decoders.EAC3;

            if (blkswe) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    planter.Write(blksw[channel]);
                }
            }
            if (dithflage) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    planter.Write(dithflag[channel]);
                }
            }

            planter.Write(dynrng, 8);
            if (header.ChannelMode == 0) {
                planter.Write(dynrng2, 8);
            }

            if (eac3) {
                WriteSPX(planter, block);
            }

            EncodeCouplingStrategy(planter, eac3, block);
            if (cplinu[block]) {
                EncodeCouplingCoordinates(planter, eac3);
            }

            if (header.ChannelMode == 2) {
                throw new UnsupportedFeatureException("stereo");
            }

            // Exponent strategy
            if (!eac3) {
                if (cplinu[block]) {
                    planter.Write((int)cplexpstr[block], 2);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    planter.Write((int)chexpstr[block][channel], 2);
                }
                if (header.LFE) {
                    planter.Write(lfeexpstr[block]);
                }
            }

            // Channel bandwidth code
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chexpstr[block][channel] != ExpStrat.Reuse && !chincpl[channel] && !chinspx[channel]) {
                    planter.Write(chbwcod[channel], 6);
                }
            }

            // Exponents
            if (cplinu[block] && cplexpstr[block] != ExpStrat.Reuse) {
                planter.Write(cplexps[0], 4);
                for (int group = 0; group < ncplgrps;) {
                    planter.Write(cplexps[++group], 7);
                }
            }

            // Exponents for full bandwidth channels
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chexpstr[block][channel] != ExpStrat.Reuse) {
                    planter.Write(allocation[channel].absoluteExponent, 4);
                    int[] exps = allocation[channel].groupedExponents;
                    for (int group = 0; group < nchgrps[channel]; group++) {
                        planter.Write(exps[group], 7);
                    }
                    planter.Write(0, 2); // gainrng, unused
                }
            }

            // Exponents for LFE channel
            if (header.LFE && lfeexpstr[block]) {
                int[] lfeexps = lfeAllocation.groupedExponents;
                planter.Write(lfeexps[0], 4);
                planter.Write(lfeexps[1], 7);
                planter.Write(lfeexps[2], 7);
            }

            // Bit allocation parametric information
            if (bamode) {
                planter.Write(baie);
                if (baie) {
                    planter.Write(sdcycod, 2);
                    planter.Write(fdcycod, 2);
                    planter.Write(sgaincod, 2);
                    planter.Write(dbpbcod, 2);
                    planter.Write(floorcod, 3);
                }
            }

            if (snroffststr != 0) {
                if (!eac3 || block != 0) {
                    planter.Write(snroffste);
                }
                if (snroffste) {
                    planter.Write(csnroffst, 6);
                    if (!eac3) {
                        if (cplinu[block]) {
                            planter.Write(cplfsnroffst, 4);
                            planter.Write(cplfgaincod, 3);
                        }
                        for (int channel = 0; channel < channels.Length; channel++) {
                            planter.Write(fsnroffst[channel], 4);
                            planter.Write(fgaincod[channel], 3);
                        }
                        if (header.LFE) {
                            planter.Write(lfefsnroffst, 4);
                            planter.Write(lfefgaincod, 3);
                        }
                    } else if (snroffststr == 1) {
                        planter.Write(blkfsnroffst, 4);
                    } else if (snroffststr == 2) {
                        if (cplinu[block]) {
                            planter.Write(cplfsnroffst, 4);
                        }
                        for (int channel = 0; channel < channels.Length; channel++) {
                            planter.Write(fsnroffst[channel], 4);
                        }
                        if (header.LFE) {
                            planter.Write(lfefsnroffst, 4);
                        }
                    }
                }
            }

            if (eac3) {
                if (frmfgaincode) {
                    planter.Write(fgaincode);
                }
                if (fgaincode) {
                    if (cplinu[block]) {
                        planter.Write(cplfgaincod, 3);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        planter.Write(fgaincod[channel], 3);
                    }
                    if (header.LFE) {
                        planter.Write(lfefgaincod, 3);
                    }
                }

                if (header.StreamTypeOut == StreamTypes.Independent) {
                    planter.Write(convsnroffste);
                    if (convsnroffste) {
                        planter.Write(convsnroffst, 10);
                    }
                }
            }

            if (cplinu[block]) {
                if (!firstcplleak) {
                    planter.Write(cplleake);
                }
                if (cplleake) {
                    planter.Write(cplfleak, 3);
                    planter.Write(cplsleak, 3);
                }
            }

            // Delta bit allocation
            if (dbaflde || !eac3) {
                planter.Write(deltbaie);
                if (deltbaie) {
                    if (cplinu[block]) {
                        planter.Write((int)cpldeltba.enabled, 2);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        planter.Write((int)deltba[channel].enabled, 2);
                    }
                    if (cplinu[block] && cpldeltba.enabled == DeltaBitAllocationMode.NewInfoFollows) {
                        cpldeltba.Write(planter);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        if (deltba[channel].enabled == DeltaBitAllocationMode.NewInfoFollows) {
                            deltba[channel].Write(planter);
                        }
                    }
                }
            }

            // "Unused dummy data" that might just be used to transport objects
            if (skipFieldSyntaxEnabled) {
                planter.Write(skipLengthEnabled);
                if (skipLengthEnabled) {
                    throw new UnsupportedFeatureException("aux");
                }
            }

            // Quantized mantissa values - prepare for the next allocation frame
            bap1Pos = 2;
            bap2Pos = 2;
            bap4Pos = 1;

            bool got_cplchan = false;
            for (int channel = 0; channel < channels.Length; channel++) {
                if (chahtinu[channel] == 0) {
                    allocation[channel].WriteTransformCoeffs(planter);
                } else {
                    throw new UnsupportedFeatureException("AHT");
                }

                if (cplinu[block] && chincpl[channel] && !got_cplchan) {
                    if (cplahtinu == 0) {
                        couplingAllocation.WriteTransformCoeffs(planter);
                        got_cplchan = true;
                    } else {
                        throw new UnsupportedFeatureException("AHT");
                    }
                }
            }

            // Combined mantissa and output handling for LFE
            if (header.LFE) {
                if (lfeahtinu == 0) {
                    lfeAllocation.WriteTransformCoeffs(planter);
                } else {
                    throw new UnsupportedFeatureException("AHT");
                }
            }
        }
    }
}