using System;

using Cavern.Format.Decoders.EnhancedAC3;

namespace Cavern.Format.Decoders {
    partial class EnhancedAC3Decoder {
        struct Allocation {
            public int[] dexp;
            public int[] aexp;
            public int[] exp;
            public int[] psd;
            public int[] bndpsd;
            public int[] excite;
            public int[] mask;
            public byte[] bap;

            public Allocation(int maxLength) {
                dexp = new int[maxLength];
                aexp = new int[maxLength];
                exp = new int[maxLength];
                psd = new int[maxLength];
                bndpsd = new int[maxLength];
                excite = new int[maxLength];
                mask = new int[maxLength];
                bap = new byte[maxLength];
            }
        }

        byte[] Allocate(int channel, int[] gexp, ExpStrat expstr) {
            int start = strtmant[channel];
            int end = endmant[channel];
            int fgain = fastgain[fgaincod[channel]];
            if (csnroffst == 0 && fsnroffst[channel] == 0)
                return new byte[nchmant[channel]];
            int snroffset = (csnroffst - 15) << 4 + fsnroffst[channel] << 2;
            return Allocate(start, end, fgain, snroffset, gexp, nchgrps[channel], exps[channel][0],
                expstr, allocation[channel], deltba[channel]);
        }

        byte[] AllocateCoupling(ExpStrat expstr) {
            int fgain = fastgain[cplfgaincod];
            if (csnroffst == 0 && lfefsnroffst == 0)
                return new byte[nlfemant];
            int snroffset = (csnroffst - 15) << 4 + cplfsnroffst << 2;
            return Allocate(cplstrtmant, cplendmant, fgain, snroffset, cplexps, ncplgrps, cplabsexp,
                expstr, couplingAllocation, cpldeltba, (cplfleak << 8) + 768, (cplsleak << 8) + 768);
        }

        byte[] AllocateLFE(int[] gexp, ExpStrat expstr) {
            int fgain = fastgain[lfefgaincod];
            if (csnroffst == 0 && lfefsnroffst == 0)
                return new byte[nlfemant];
            int snroffset = (csnroffst - 15) << 4 + lfefsnroffst << 2;
            return Allocate(lfestrtmant, lfeendmant, fgain, snroffset, gexp, nlfegrps, lfeexps[0],
                expstr, lfeAllocation, lfedeltba);
        }

        byte[] Allocate(int start, int end, int fgain, int snroffset, int[] gexp, int ngrps, int absexp,
            ExpStrat expstr, Allocation allocation, DeltaBitAllocation dba, int fastleak = 0, int slowleak = 0) {
            // Unpack the mapped values
            int[] dexp = allocation.dexp;
            for (int grp = 0; grp < ngrps; ++grp) {
                int expacc = gexp[grp];
                dexp[grp * 3] = expacc / 25;
                expacc -= 25 * dexp[grp * 3];
                dexp[grp * 3 + 1] = expacc / 5;
                expacc -= (5 * dexp[grp * 3 + 1]);
                dexp[grp * 3 + 2] = expacc;
            }

            // Unbiased mapped values
            for (int grp = 0; grp < ngrps * 3; ++grp)
                dexp[grp] = dexp[grp] - 2;

            // Convert from differentials to absolutes
            int i, j;
            int[] aexp = allocation.aexp;
            int prevexp = absexp;
            for (i = 0; i < (ngrps * 3); ++i) {
                aexp[i] = prevexp + dexp[i];
                prevexp = aexp[i];
            }

            // Expand to full absolute exponent array, using grpsize
            int grpsize = (int)expstr;
            if (grpsize == (int)ExpStrat.D45)
                grpsize = 4;
            int[] exp = allocation.exp;
            exp[0] = absexp;
            for (i = 0; i < (ngrps * 3); ++i)
                for (j = 0; j < grpsize; ++j)
                    exp[i * grpsize + j + 1] = aexp[i];

            // Initialization
            int sdecay = slowdec[sdcycod];
            int fdecay = fastdec[fdcycod];
            int sgain = slowgain[sgaincod];
            int dbknee = dbpbtab[dbpbcod];
            int floor = floortab[floorcod];

            // Exponent mapping into psd
            int[] psd = allocation.psd;
            for (int bin = start; bin < end; ++bin)
                psd[bin] = 3072 - (exp[bin] << 7);

            // psd integration
            int[] bndpsd = allocation.bndpsd;
            j = start;
            int k = masktab[start];
            int lastbin;
            do {
                lastbin = Math.Min(bndtab[k] + bndsz[k], end);
                bndpsd[k] = psd[j];
                ++j;
                for (i = j; i < lastbin; ++i) {
                    bndpsd[k] = LogAdd(bndpsd[k], psd[j]);
                    ++j;
                }
                ++k;
            }
            while (end > lastbin);

            // Compute excitation function
            int[] excite = allocation.excite;
            int bndstrt = masktab[start];
            int bndend = masktab[end - 1] + 1;
            int begin;
            int lowcomp = 0;
            if (bndstrt == 0) { // For full bandwidth and LFE channels
                lowcomp = CalcLowcomp(lowcomp, bndpsd[0], bndpsd[1], 0);
                excite[0] = bndpsd[0] - fgain - lowcomp;
                lowcomp = CalcLowcomp(lowcomp, bndpsd[1], bndpsd[2], 1);
                excite[1] = bndpsd[1] - fgain - lowcomp;
                begin = 7;
                for (int bin = 2; bin < 7; ++bin) {
                    if ((bndend != 7) || (bin != 6))
                        lowcomp = CalcLowcomp(lowcomp, bndpsd[bin], bndpsd[bin + 1], bin);
                    fastleak = bndpsd[bin] - fgain;
                    slowleak = bndpsd[bin] - sgain;
                    excite[bin] = fastleak - lowcomp;
                    if ((bndend != 7) || (bin != 6)) {
                        if (bndpsd[bin] <= bndpsd[bin + 1]) {
                            begin = bin + 1;
                            break;
                        }
                    }
                }
                for (int bin = begin; bin < Math.Min(bndend, 22); ++bin) {
                    if ((bndend != 7) || (bin != 6))
                        lowcomp = CalcLowcomp(lowcomp, bndpsd[bin], bndpsd[bin + 1], bin);
                    fastleak -= fdecay;
                    fastleak = Math.Max(fastleak, bndpsd[bin] - fgain);
                    slowleak -= sdecay;
                    slowleak = Math.Max(slowleak, bndpsd[bin] - sgain);
                    excite[bin] = Math.Max(fastleak - lowcomp, slowleak);
                }
                begin = 22;
            } else /* For coupling channel */
                begin = bndstrt;
            for (int bin = begin; bin < bndend; ++bin) {
                fastleak -= fdecay;
                fastleak = Math.Max(fastleak, bndpsd[bin] - fgain);
                slowleak -= sdecay;
                slowleak = Math.Max(slowleak, bndpsd[bin] - sgain);
                excite[bin] = Math.Max(fastleak, slowleak);
            }

            // Compute masking curve
            int[] mask = allocation.mask;
            for (int bin = bndstrt; bin < bndend; ++bin) {
                if (bndpsd[bin] < dbknee)
                    excite[bin] += (dbknee - bndpsd[bin]) >> 2;
                mask[bin] = Math.Max(excite[bin], hth[header.SampleRateCode][bin]);
            }

            // Apply delta bit allocation
            if (dba.enabled == DeltaBitAllocationMode.Reuse || dba.enabled == DeltaBitAllocationMode.NewInfoFollows) {
                int band = 0;
                int[] offset = dba.Offset;
                int[] length = dba.Length;
                int[] bitAllocation = dba.BitAllocation;
                for (int seg = 0; seg < offset.Length; ++seg) {
                    band += offset[seg];
                    int delta = bitAllocation[seg] >= 4 ? (bitAllocation[seg] - 3) << 7 : ((bitAllocation[seg] - 4) << 7);
                    for (k = 0; k < length[seg]; ++k) {
                        mask[band] += delta;
                        ++band;
                    }
                }
            }

            // Compute bit allocation
            byte[] bap = allocation.bap;
            i = start;
            j = masktab[start];
            do {
                lastbin = Math.Min(bndtab[j] + bndsz[j], end);
                mask[j] -= snroffset;
                mask[j] -= floor;
                if (mask[j] < 0) {
                    mask[j] = 0;
                }
                mask[j] &= 0x1fe0;
                mask[j] += floor;
                for (k = i; k < lastbin; k++) {
                    int address = (psd[i] - mask[j]) >> 5;
                    address = Math.Min(63, Math.Max(0, address));
                    bap[i] = baptab[address];
                    i++;
                }
                j++;
            }
            while (end > lastbin);
            return bap;
        }

        int LogAdd(int a, int b) {
            int c = a - b;
            int address = Math.Min(Math.Abs(c) >> 1, 255);
            if (c >= 0)
                return a + latab[address];
            return b + latab[address];
        }

        int CalcLowcomp(int a, int b0, int b1, int bin) {
            if (bin < 7) {
                if ((b0 + 256) == b1)
                    a = 384;
                else if (b0 > b1)
                    a = Math.Max(0, a - 64);
            } else if (bin < 20) {
                if ((b0 + 256) == b1)
                    a = 320;
                else if (b0 > b1)
                    a = Math.Max(0, a - 64);
            } else
                a = Math.Max(0, a - 128);
            return a;
        }
    }
}