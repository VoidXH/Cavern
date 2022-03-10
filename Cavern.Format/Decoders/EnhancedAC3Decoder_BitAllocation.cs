using System;

using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    internal partial class EnhancedAC3Decoder {
        class BitAllocation {
            public readonly int sdcycod,
                fdcycod,
                sgaincod,
                dbpbcod,
                floorcod,
                lfefsnroffst,
                csnroffst,
                lfefgaincod;
            public readonly int[] fsnroffst,
                fgaincod;

            public BitAllocation(BitExtractor extractor, int block, int channels, bool LFE,
                bool bamode, int snroffststr, int frmfsnroffst, bool frmfgaincode) {
                fsnroffst = new int[channels];
                fgaincod = new int[channels];

                if (bamode) {
                    if (extractor.ReadBit()) {
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
                    for (int channel = 0; channel < channels; ++channel)
                        fsnroffst[channel] = frmfsnroffst;
                    if (LFE)
                        lfefsnroffst = frmfsnroffst;
                } else {
                    bool snroffste = true;
                    if (block != 0)
                        snroffste = extractor.ReadBit();
                    if (snroffste) {
                        csnroffst = extractor.Read(6);
                        if (snroffststr == 1) {
                            int blkfsnroffst = extractor.Read(4);
                            for (int channel = 0; channel < channels; ++channel)
                                fsnroffst[channel] = blkfsnroffst;
                            lfefsnroffst = blkfsnroffst;
                        } else if (snroffststr == 2) {
                            for (int channel = 0; channel < channels; ++channel)
                                fsnroffst[channel] = extractor.Read(4);
                            if (LFE)
                                lfefsnroffst = extractor.Read(4);
                        }
                    }
                }

                if (frmfgaincode && extractor.ReadBit()) {
                    for (int channel = 0; channel < channels; ++channel)
                        fgaincod[channel] = extractor.Read(3);
                    if (LFE)
                        lfefgaincod = extractor.Read(3);
                } else {
                    for (int channel = 0; channel < channels; ++channel)
                        fgaincod[channel] = 4;
                    if (LFE)
                        lfefgaincod = 4;
                }
            }

            public int[] Allocate(int[] endmant, int channel, int ngrps, int[] gexp, ExponentStrategies expstr) {
                int end = endmant[channel];
                int fgain = fastgain[fgaincod[channel]];
                int snroffset = (csnroffst - 15) << 4 + fsnroffst[channel] << 2;
                return Allocate(end, fgain, snroffset, ngrps, gexp, expstr);
            }

            public int[] AllocateLFE(int ngrps, int[] gexp, ExponentStrategies expstr) {
                int end = 7;
                int fgain = fastgain[lfefgaincod];
                int snroffset = (csnroffst - 15) << 4 + lfefsnroffst << 2;
                return Allocate(end, fgain, snroffset, ngrps, gexp, expstr);
            }

            int[] Allocate(int end, int fgain, int snroffset, int ngrps, int[] gexp, ExponentStrategies expstr) {
                int sdecay = slowdec[sdcycod];
                int fdecay = fastdec[fdcycod];
                int sgain = slowgain[sgaincod];
                int dbknee = dbpbtab[dbpbcod];
                int floor = floortab[floorcod];
                int start = 0;
                int lowcomp = 0;

                int[] psd = new int[end], exp = Exponents(ngrps, gexp, expstr);
                for (int bin = start; bin < end; bin++)
                    psd[bin] = (3072 - (exp[bin] << 7));

                int j = start;
                int k = masktab[start];
                int lastbin;
                int[] bndpsd = new int[256];
                do {
                    lastbin = Math.Min(bndtab[k] + bndsz[k], end);
                    bndpsd[k] = psd[j];
                    ++j;
                    for (int i = j; i < lastbin; ++i) {
                        bndpsd[k] = LogAdd(bndpsd[k], psd[j]);
                        ++j;
                    }
                    ++k;
                } while (end > lastbin);

                return null; // TODO
            }

            int[] Exponents(int ngrps, int[] gexp, ExponentStrategies expstr) {
                // Unpack the mapped values
                int[] dexp = new int[ngrps * 3];
                for (int grp = 0; grp < ngrps; ++grp) {
                    int expacc = gexp[grp];
                    dexp[grp * 3] = expacc / 25;
                    expacc -= 25 * dexp[grp * 3];
                    dexp[grp * 3 + 1] = expacc / 5;
                    expacc -= 5 * dexp[grp * 3 + 1];
                    dexp[grp * 3 + 2] = expacc;
                }

                // Unbiased mapped values
                for (int grp = 0; grp < ngrps * 3; ++grp)
                    dexp[grp] -= 2;

                // Convert from differentials to absolutes
                int[] aexp = new int[ngrps * 3];
                int prevexp = gexp[0];
                for (int i = 0; i < ngrps * 3; ++i) {
                    aexp[i] = prevexp + dexp[i];
                    prevexp = aexp[i];
                }

                // Expand to full absolute exponent array, using grpsize
                if (expstr == ExponentStrategies.Reuse)
                    expstr = ExponentStrategies.D15;
                int grpsize = (int)expstr;
                if (expstr == ExponentStrategies.D45)
                    grpsize = 4;
                int[] exp = new int[ngrps * 3 * grpsize + 1];
                exp[0] = gexp[0];
                for (int i = 0; i < ngrps * 3; ++i)
                    for (int j = 0; j < grpsize; ++j)
                        exp[i * grpsize + j + 1] = aexp[i];
                return exp;
            }

            int LogAdd(int a, int b) {
                int c = a + b;
                int address = Math.Min(Math.Abs(c) >> 1, 255);
                if (c >= 0)
                    return a + latab[address];
                return b + latab[address];
            }

            /// <summary>
            /// Slow decay table.
            /// </summary>
            static readonly int[] slowdec = new int[4] { 0x0f, 0x11, 0x13, 0x15 };

            /// <summary>
            /// Fast decay table.
            /// </summary>
            static readonly int[] fastdec = new int[4] { 0x3f, 0x53, 0x67, 0x7b };

            /// <summary>
            /// Slow gain table.
            /// </summary>
            static readonly int[] slowgain = new int[4] { 0x540, 0x4d8, 0x478, 0x410 };

            /// <summary>
            /// dB/bit table.
            /// </summary>
            static readonly int[] dbpbtab = new int[4] { 0x000, 0x700, 0x900, 0xb00 };

            /// <summary>
            /// Floor table.
            /// </summary>
            static readonly int[] floortab = new int[8] { 0x2f0, 0x2b0, 0x270, 0x230, 0x1f0, 0x170, 0x0f0, 0xf800 };

            /// <summary>
            /// Fast gain table.
            /// </summary>
            static readonly int[] fastgain = new int[8] { 0x080, 0x100, 0x180, 0x200, 0x280, 0x300, 0x380, 0x400 };

            /// <summary>
            /// Banding structure tables.
            /// </summary>
            static readonly int[] bndtab = new int[50] {
                0,   1,  2,   3,   4,   5,   6,   7,   8,   9,
                10, 11, 12,  13,  14,  15,  16,  17,  18,  19,
                20, 21, 22,  23,  24,  25,  26,  27,  28,  31,
                34, 37, 40,  43,  46,  49,  55,  61,  67,  73,
                79, 85, 97, 109, 121, 133, 157, 181, 205, 229
            };

            /// <summary>
            /// Banding structure tables.
            /// </summary>
            static readonly int[] bndsz = new int[50] {
                1, 1,  1,  1,  1,  1,  1,  1,  1,  1,
                1, 1,  1,  1,  1,  1,  1,  1,  1,  1,
                1, 1,  1,  1,  1,  1,  1,  1,  3,  3,
                3, 3,  3,  3,  3,  6,  6,  6,  6,  6,
                6, 12, 12, 12, 12, 24, 24, 24, 24, 24
            };

            /// <summary>
            /// Bin number to band number table.
            /// </summary>
            static readonly int[] masktab = new int[256] {
                0,   1,  2,  3,  4,  5,  6,  7,  8,  9,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                20, 21, 22, 23, 24, 25, 26, 27, 28, 28,
                28, 29, 29, 29, 30, 30, 30, 31, 31, 31,
                32, 32, 32, 33, 33, 33, 34, 34, 34, 35,
                35, 35, 35, 35, 35, 36, 36, 36, 36, 36,
                36, 37, 37, 37, 37, 37, 37, 38, 38, 38,
                38, 38, 38, 39, 39, 39, 39, 39, 39, 40,
                40, 40, 40, 40, 40, 41, 41, 41, 41, 41,
                41, 41, 41, 41, 41, 41, 41, 42, 42, 42,
                42, 42, 42, 42, 42, 42, 42, 42, 42, 43,
                43, 43, 43, 43, 43, 43, 43, 43, 43, 43,
                43, 44, 44, 44, 44, 44, 44, 44, 44, 44,
                44, 44, 44, 45, 45, 45, 45, 45, 45, 45,
                45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
                45, 45, 45, 45, 45, 45, 45, 46, 46, 46,
                46, 46, 46, 46, 46, 46, 46, 46, 46, 46,
                46, 46, 46, 46, 46, 46, 46, 46, 46, 46,
                46, 47, 47, 47, 47, 47, 47, 47, 47, 47,
                47, 47, 47, 47, 47, 47, 47, 47, 47, 47,
                47, 47, 47, 47, 47, 48, 48, 48, 48, 48,
                48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
                48, 48, 48, 48, 48, 48, 48, 48, 48, 49,
                49, 49, 49, 49, 49, 49, 49, 49, 49, 49,
                49, 49, 49, 49, 49, 49, 49, 49, 49, 49,
                49, 49, 49, 0, 0, 0
            };

            /// <summary>
            /// Log-addition table.
            /// </summary>
            static readonly int[] latab = new int[260] {
                0x40, 0x3f, 0x3e, 0x3d, 0x3c, 0x3b, 0x3a, 0x39, 0x38, 0x37,
                0x36, 0x35, 0x34, 0x34, 0x33, 0x32, 0x31, 0x30, 0x2f, 0x2f,
                0x2e, 0x2d, 0x2c, 0x2c, 0x2b, 0x2a, 0x29, 0x29, 0x28, 0x27,
                0x26, 0x26, 0x25, 0x24, 0x24, 0x23, 0x23, 0x22, 0x21, 0x21,
                0x20, 0x20, 0x1f, 0x1e, 0x1e, 0x1d, 0x1d, 0x1c, 0x1c, 0x1b,
                0x1b, 0x1a, 0x1a, 0x19, 0x19, 0x18, 0x18, 0x17, 0x17, 0x16,
                0x16, 0x15, 0x15, 0x15, 0x14, 0x14, 0x13, 0x13, 0x13, 0x12,
                0x12, 0x12, 0x11, 0x11, 0x11, 0x10, 0x10, 0x10, 0x0f, 0x0f,
                0x0f, 0x0e, 0x0e, 0x0e, 0x0d, 0x0d, 0x0d, 0x0d, 0x0c, 0x0c,
                0x0c, 0x0c, 0x0b, 0x0b, 0x0b, 0x0b, 0x0a, 0x0a, 0x0a, 0x0a,
                0x0a, 0x09, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08, 0x08,
                0x08, 0x08, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x06, 0x06,
                0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x05, 0x05, 0x05, 0x05,
                0x05, 0x05, 0x05, 0x05, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
                0x04, 0x04, 0x04, 0x04, 0x04, 0x03, 0x03, 0x03, 0x03, 0x03,
                0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x02,
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
        }
    }
}