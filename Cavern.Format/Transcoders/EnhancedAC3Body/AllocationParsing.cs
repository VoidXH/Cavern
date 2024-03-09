using System;
using System.Runtime.CompilerServices;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        // Convert bitstream data to allocation data
        partial class Allocation {
            /// <summary>
            /// Read grouped full-range channel exponent data from the bitstream and decode it.
            /// </summary>
            public void ReadChannelExponents(BitExtractor extractor, ExpStrat expstr, int nchgrps) {
                ReadExponents(extractor, nchgrps);
                extractor.Skip(2); // This is gainrng, telling the max gain as 1/(2^gainrng). It's useless.
                UngroupExponents(nchgrps, expstr, 0, 1);
            }

            /// <summary>
            /// Read grouped coupling channel exponent data from the bitstream and decode it.
            /// </summary>
            public void ReadCouplingExponents(BitExtractor extractor, ExpStrat expstr, int startMantissa, int ncplgrps) {
                ReadExponents(extractor, ncplgrps);
                absoluteExponent <<= 1;
                UngroupExponents(ncplgrps, expstr, startMantissa, startMantissa);
            }

            /// <summary>
            /// Read grouped LFE exponent data from the bitstream and decode it.
            /// </summary>
            public void ReadLFEExponents(BitExtractor extractor) {
                absoluteExponent = extractor.Read(4);
                groupedExponents[0] = extractor.Read(7);
                groupedExponents[1] = extractor.Read(7);
                UngroupExponents(nlfegrps, ExpStrat.D15, lfestrtmant, lfestrtmant + 1);
            }

            /// <summary>
            /// Read the grouped exponents from the bitstream.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ReadExponents(BitExtractor extractor, int ngrps) {
                absoluteExponent = extractor.Read(4);
                for (int group = 0; group < ngrps; group++) {
                    groupedExponents[group] = extractor.Read(7);
                }
            }

            /// <summary>
            /// Ungroup what's grouped in <see cref="groupedExponents"/>, decode the differential coding,
            /// and calculate power spectral density values.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UngroupExponents(int ngrps, ExpStrat expstr, int startMantissa, int exponentOffset) {
                // Ungrouping and decoding exponents
                int grpsize = expstr != ExpStrat.D45 ? (int)expstr : 4,
                    absexp = absoluteExponent, // Rolling differential decoding value (dexp in the reference code)
                    endMantissa = exponentOffset;
                exponents[0] = absexp;
                for (int grp = 0; grp < ngrps; grp++) {
                    int expacc = groupedExponents[grp];
                    absexp += expacc / 25 - 2; // Ungroup and unbias mapped values in the same step
                    for (int j = 0; j < grpsize; j++) {
                        exponents[endMantissa++] = absexp;
                    }

                    absexp += expacc % 25 / 5 - 2;
                    for (int j = 0; j < grpsize; j++) {
                        exponents[endMantissa++] = absexp;
                    }

                    absexp += expacc % 5 - 2;
                    for (int j = 0; j < grpsize; j++) {
                        exponents[endMantissa++] = absexp;
                    }
                }

                // Exponent mapping into PSD
                for (int bin = startMantissa; bin < endMantissa; bin++) {
                    psd[bin] = 3072 - (exponents[bin] << 7);
                }

                // PSD integration
                int i = startMantissa,
                    k = masktab[startMantissa],
                    lastbin;
                do {
                    lastbin = Math.Min(bndtab[k], endMantissa);
                    integratedPSD[k] = psd[i++];
                    while (i < lastbin) {
                        integratedPSD[k] = LogAdd(integratedPSD[k], psd[i++]);
                    }
                    k++;
                } while (endMantissa > lastbin);
            }
        }
    }
}