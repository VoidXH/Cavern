using System;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    // Functions related to the coupling channel, its decoding/encoding, and its application.
    partial class EnhancedAC3Body {
        /// <summary>
        /// Decode the (enhanced) coupling strategy information.
        /// </summary>
        void DecodeCouplingStrategy(bool eac3, int block) {
            if (!eac3 && (cplstre[block] = extractor.ReadBit())) {
                cplinu[block] = extractor.ReadBit();
            }

            if (cplstre[block] || !eac3) {
                if (cplinu[block]) {
                    ecplinu = eac3 && extractor.ReadBit();
                    if (eac3 && header.ChannelMode == 2) {
                        chincpl[0] = chincpl[1] = true;
                    } else {
                        for (int channel = 0; channel < channels.Length; channel++) {
                            chincpl[channel] = extractor.ReadBit();
                        }
                    }
                    if (!ecplinu) { // Standard coupling
                        if (header.ChannelMode == 0x2) {
                            phsflginu = extractor.ReadBit();
                        }
                        cplbegf = extractor.Read(4);
                        if (!spxinu) {
                            cplendf = extractor.Read(4);
                        } else {
                            cplendf = spxbegf < 6 ? spxbegf - 2 : (spxbegf * 2 - 7);
                        }
                        ncplsubnd = 3 + cplendf - cplbegf;
                        ncplbnd = ncplsubnd;
                        if (ncplsubnd < 1) {
                            throw new DecoderException(3);
                        }
                        if (cplbndstrce = !eac3 || extractor.ReadBit()) {
                            for (int band = 1; band < ncplsubnd; band++) {
                                if (cplbndstrc[cplbegf + band] = extractor.ReadBit()) {
                                    --ncplbnd;
                                }
                            }
                        } else {
                            for (int band = 1; band < ncplsubnd; band++) {
                                if (cplbndstrc[cplbegf + band]) {
                                    --ncplbnd;
                                }
                            }
                        }
                    } else { // Enhanced coupling
                        ecplbegf = extractor.Read(4);
                        if (ecplbegf < 3) {
                            ecpl_begin_subbnd = ecplbegf * 2;
                        } else if (ecplbegf < 13) {
                            ecpl_begin_subbnd = ecplbegf + 2;
                        } else {
                            ecpl_begin_subbnd = ecplbegf * 2 - 10;
                        }
                        if (!spxinu) {
                            ecplendf = extractor.Read(4);
                            ecpl_end_subbnd = ecplendf + 7;
                        } else {
                            ecpl_end_subbnd = spxbegf < 6 ? spxbegf + 5 : (spxbegf * 2);
                        }
                        if (ecplbndstrce = extractor.ReadBit()) {
                            Array.Clear(ecplbndstrc, 0, ecplbndstrc.Length);
                            for (int sbnd = Math.Max(9, ecpl_begin_subbnd + 1); sbnd < ecpl_end_subbnd; sbnd++) {
                                ecplbndstrc[sbnd] = extractor.ReadBit();
                            }
                        }
                    }
                } else if (eac3) {
                    for (int channel = 0; channel < channels.Length; channel++) {
                        chincpl[channel] = false;
                        firstcplcos[channel] = true;
                    }
                    firstcplleak = true;
                    phsflginu = false;
                    ecplinu = false;
                }
            }
        }

        /// <summary>
        /// Decode coupling coordinates and phase flags.
        /// </summary>
        void DecodeCouplingCoordinates(bool eac3) {
            if (!ecplinu) { // Standard coupling
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (chincpl[channel]) {
                        if (eac3 && firstcplcos[channel]) {
                            cplcoe[channel] = true;
                            firstcplcos[channel] = false;
                        } else {
                            cplcoe[channel] = extractor.ReadBit();
                        }
                        if (cplcoe[channel]) {
                            mstrcplco[channel] = extractor.Read(2) * 3;
                            int[] tcplco = cplco[channel],
                                cplchexp = cplcoexp[channel],
                                cplchmant = cplcomant[channel];
                            for (int band = 0; band < ncplbnd; band++) {
                                cplchexp[band] = extractor.Read(4);
                                cplchmant[band] = extractor.Read(4);
                                if (cplchexp[band] != 15) {
                                    tcplco[band] = (cplchmant[band] + 16) << (15 - cplchexp[band] - mstrcplco[channel]);
                                } else {
                                    tcplco[band] = cplchmant[band] << (15 - mstrcplco[channel]);
                                }
                            }
                        }
                        if ((header.ChannelMode == 0x2) && phsflginu && (cplcoe[0] || cplcoe[1])) {
                            throw new UnsupportedFeatureException("stereo");
                        }
                    } else {
                        firstcplcos[channel] = true;
                    }
                }
            } else { // Enhanced coupling
                throw new UnsupportedFeatureException("ecplinu");
            }
        }

        /// <summary>
        /// Encode the (enhanced) coupling strategy information.
        /// </summary>
        void EncodeCouplingStrategy(BitPlanter planter, bool eac3, int block) {
            if (!eac3) {
                planter.Write(cplstre[block]);
                if (cplstre[block]) {
                    planter.Write(cplinu[block]);
                }
            }

            if (cplstre[block] || !eac3) {
                if (cplinu[block]) {
                    if (eac3) {
                        planter.Write(ecplinu);
                    }
                    if (!eac3 || header.ChannelMode != 2) {
                        for (int channel = 0; channel < channels.Length; channel++) {
                            planter.Write(chincpl[channel]);
                        }
                    }
                    if (!ecplinu) { // Standard coupling
                        if (header.ChannelMode == 0x2) {
                            planter.Write(phsflginu);
                        }
                        planter.Write(cplbegf, 4);
                        if (!spxinu) {
                            planter.Write(cplendf, 4);
                        }
                        if (eac3) {
                            planter.Write(cplbndstrce);
                        }
                        if (cplbndstrce) {
                            for (int band = 1; band < ncplsubnd; band++) {
                                planter.Write(cplbndstrc[cplbegf + band]);
                            }
                        }
                    } else { // Enhanced coupling
                        throw new UnsupportedFeatureException("ecplinu");
                    }
                } else if (eac3) {
                    for (int channel = 0; channel < channels.Length; channel++) {
                        chincpl[channel] = false;
                        firstcplcos[channel] = true;
                    }
                    firstcplleak = true;
                    phsflginu = false;
                    ecplinu = false;
                }
            }
        }

        /// <summary>
        /// Encode coupling coordinates and phase flags.
        /// </summary>
        void EncodeCouplingCoordinates(BitPlanter planter, bool eac3) {
            if (!ecplinu) { // Standard coupling
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (chincpl[channel]) {
                        if (eac3 && firstcplcos[channel]) {
                            cplcoe[channel] = true;
                            firstcplcos[channel] = false;
                        } else {
                            planter.Write(cplcoe[channel]);
                        }
                        if (cplcoe[channel]) {
                            planter.Write(mstrcplco[channel] / 3, 2);
                            int[] cplchexp = cplcoexp[channel],
                                cplchmant = cplcomant[channel];
                            for (int band = 0; band < ncplbnd; band++) {
                                planter.Write(cplchexp[band], 4);
                                planter.Write(cplchmant[band], 4);
                            }
                        }
                        if ((header.ChannelMode == 2) && phsflginu && (cplcoe[0] || cplcoe[1])) {
                            throw new UnsupportedFeatureException("stereo");
                        }
                    } else {
                        firstcplcos[channel] = true;
                    }
                }
            } else { // Enhanced coupling
                throw new UnsupportedFeatureException("ecplinu");
            }
        }

        /// <summary>
        /// Add coupling with the required coordinates to a channel.
        /// </summary>
        void ApplyCoupling(int channel) {
            float[] channelMantissa = channelOutput[channel];
            int[] channelCouplingCoords = cplco[channel];
            for (int subband = 0, usedSubband = 0; subband < ncplsubnd; subband++) {
                if (!cplbndstrc[cplbegf + subband]) {
                    ++usedSubband;
                }
                int offset = (cplbegf + subband) * 12 + 37;
                for (int bin = 0; bin < 12; bin++) {
                    const float alignment = 1f / (1 << 17);
                    channelMantissa[bin + offset] =
                        couplingTransformCoeffs[bin + offset] * channelCouplingCoords[usedSubband] * alignment;
                }
            }
        }
    }
}