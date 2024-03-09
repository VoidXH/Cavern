using System;

using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        void Allocate(int channel) {
            if (csnroffst == 0 && fsnroffst[channel] == 0) {
                allocation[channel].bap.Clear();
                return;
            }
            int snroffset = (((csnroffst - 15) << 4) + fsnroffst[channel]) << 2;
            Allocate(0, endmant[channel], fastgain[fgaincod[channel]], snroffset, allocation[channel], deltba[channel], 0, 0);
        }

        void AllocateCoupling() {
            if (csnroffst == 0 && cplfsnroffst == 0) {
                couplingAllocation.bap.Clear();
                return;
            }
            int snroffset = (((csnroffst - 15) << 4) + cplfsnroffst) << 2;
            Allocate(cplstrtmant, cplendmant, fastgain[cplfgaincod], snroffset, couplingAllocation, cpldeltba,
                (cplfleak << 8) + 768, (cplsleak << 8) + 768);
        }

        void AllocateLFE() {
            if (csnroffst == 0 && lfefsnroffst == 0) {
                Array.Clear(lfeAllocation.bap, 0, lfeAllocation.bap.Length);
                return;
            }
            int snroffset = (((csnroffst - 15) << 4) + lfefsnroffst) << 2;
            Allocate(lfestrtmant, lfeendmant, fastgain[lfefgaincod], snroffset, lfeAllocation, lfedeltba, 0, 0);
        }

        void Allocate(int start, int end, int fgain, int snroffset,
            Allocation allocation, DeltaBitAllocation dba, int fastleak, int slowleak) {
            // Initialization
            int sdecay = slowdec[sdcycod];
            int fdecay = fastdec[fdcycod];
            int sgain = slowgain[sgaincod];
            int dbknee = dbpbtab[dbpbcod];
            int floor = floortab[floorcod];

            // Compute excitation function
            int[] psd = allocation.psd,
                bndpsd = allocation.integratedPSD,
                excite = allocation.excite;
            int bndstrt = masktab[start];
            int bndend = masktab[end - 1] + 1;
            int begin;
            if (bndstrt == 0) { // Full bandwidth and LFE channels
                int lowcomp = CalcLowcomp(0, bndpsd[0], bndpsd[1], 0);
                excite[0] = bndpsd[0] - fgain - lowcomp;
                lowcomp = CalcLowcomp(lowcomp, bndpsd[1], bndpsd[2], 1);
                excite[1] = bndpsd[1] - fgain - lowcomp;
                begin = 7;
                for (int bin = 2; bin < 7; bin++) {
                    if (bndend != 7 || bin != 6) {
                        lowcomp = CalcLowcomp(lowcomp, bndpsd[bin], bndpsd[bin + 1], bin);
                    }
                    fastleak = bndpsd[bin] - fgain;
                    slowleak = bndpsd[bin] - sgain;
                    excite[bin] = fastleak - lowcomp;
                    if ((bndend != 7 || bin != 6) && bndpsd[bin] <= bndpsd[bin + 1]) {
                        begin = bin + 1;
                        break;
                    }
                }
                for (int bin = begin, bins = Math.Min(bndend, 22); bin < bins; bin++) {
                    if (bndend != 7 || bin != 6) {
                        lowcomp = CalcLowcomp(lowcomp, bndpsd[bin], bndpsd[bin + 1], bin);
                    }
                    fastleak = Math.Max(fastleak - fdecay, bndpsd[bin] - fgain);
                    slowleak = Math.Max(slowleak - sdecay, bndpsd[bin] - sgain);
                    excite[bin] = Math.Max(fastleak - lowcomp, slowleak);
                }
                begin = 22;
            } else { // Coupling channel
                begin = bndstrt;
            }
            for (int bin = begin; bin < bndend; bin++) {
                fastleak = Math.Max(fastleak - fdecay, bndpsd[bin] - fgain);
                slowleak = Math.Max(slowleak - sdecay, bndpsd[bin] - sgain);
                excite[bin] = Math.Max(fastleak, slowleak);
            }

            // Compute masking curve
            int[] mask = allocation.mask;
            for (int bin = bndstrt; bin < bndend; bin++) {
                if (bndpsd[bin] < dbknee) {
                    excite[bin] += (dbknee - bndpsd[bin]) >> 2;
                }
                mask[bin] = Math.Max(excite[bin], hth[header.SampleRateCode][bin]);
            }

            // Apply delta bit allocation
            if (dba.enabled == DeltaBitAllocationMode.Reuse || dba.enabled == DeltaBitAllocationMode.NewInfoFollows) {
                int[] offset = dba.Offset;
                int[] length = dba.Length;
                int[] bitAllocation = dba.BitAllocation;
                for (int band = bndstrt, seg = 0; seg < offset.Length; seg++) {
                    band += offset[seg];
                    int delta = bitAllocation[seg] >= 4 ? (bitAllocation[seg] - 3) << 7 : ((bitAllocation[seg] - 4) << 7);
                    for (int k = 0; k < length[seg]; k++) {
                        mask[band++] += delta;
                    }
                }
            }

            // Compute bit allocation
            byte[] bap = allocation.bap;
            int i = start,
                j = masktab[start],
                lastbin;
            do {
                lastbin = Math.Min(bndtab[j], end);
                int masked = mask[j] - snroffset - floor;
                if (masked < 0) {
                    masked = 0;
                }
                masked = (masked & 0x1fe0) + floor;
                while (i < lastbin) {
                    int address = (psd[i] - masked) >> 5;
                    address = Math.Min(63, Math.Max(0, address));
                    bap[i++] = baptab[address];
                }
                j++;
            }
            while (end > lastbin);
            Array.Clear(bap, i, bap.Length - i);
        }

        static int LogAdd(int a, int b) {
            int c = a - b;
            int address = Math.Min(Math.Abs(c) >> 1, 255);
            if (c >= 0) {
                return a + latab[address];
            }
            return b + latab[address];
        }

        static int CalcLowcomp(int a, int b0, int b1, int bin) {
            if (bin < 7) {
                if (b0 + 256 == b1) {
                    return 384;
                } else if (b0 > b1) {
                    return Math.Max(0, a - 64);
                }
            } else if (bin < 20) {
                if (b0 + 256 == b1) {
                    return 320;
                } else if (b0 > b1) {
                    return Math.Max(0, a - 64);
                }
            } else {
                return Math.Max(0, a - 128);
            }
            return a;
        }
    }
}