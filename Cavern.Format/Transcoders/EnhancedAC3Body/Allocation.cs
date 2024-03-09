using System;

using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Contains and decodes exponent and mantissa data for a single channels.
        /// </summary>
        partial class Allocation {
            /// <summary>
            /// A copy of the mantissa stream to be able to quickly transcode the block without re-encoding.
            /// </summary>
            byte[] rawMantissa = new byte[0];

            /// <summary>
            /// Length of the content in <see cref="rawMantissa"/> in bits.
            /// </summary>
            int mantissaBits;

            /// <summary>
            /// Read the encoded mantissa values and convert them to transform coeffs into the <paramref name="target"/> array.
            /// </summary>
            public void ReadTransformCoeffs(BitExtractor extractor, float[] target, int start, int end) {
                mantissaBits = 0; // Additionally, keep raw data in a byte array to make transcoding possible
                long bapReads = 0; // 4 bytes for the first 4 baps, counting their read count efficiently
                for (int bin = start; bin < end; ++bin) {
                    if (bap[bin] != 0 && bap[bin] < 5) {
                        bapReads += 1 << ((bap[bin] - 1) * 8);
                    } else {
                        mantissaBits += bitsToRead[bap[bin]];
                    }
                }
                mantissaBits += (int)
                    ((host.bap1Pos + (bapReads & 0xFF)) / 3 * bitsToRead[1] +
                    (host.bap2Pos + ((bapReads >> 8) & 0xFF)) / 3 * bitsToRead[2] +
                    ((bapReads >> 16) & 0xFF) * bitsToRead[3] +
                    (host.bap4Pos + (bapReads >> 24)) / 2 * bitsToRead[4]);
                extractor.ReadBitsInto(ref rawMantissa, mantissaBits);
                DecodeTransformCoeffs(new BitExtractor(rawMantissa), target, start, end);
                Array.Clear(target, 0, start);
                Array.Clear(target, end, target.Length - end);
            }

            /// <summary>
            /// Write the encoded mantissa values to a bitstream.
            /// </summary>
            public void WriteTransformCoeffs(BitPlanter planter) => planter.WriteBits(rawMantissa, mantissaBits);

            /// <summary>
            /// Performs a 512-sample inverse modified discrete cosine transform in-place.
            /// </summary>
            public void IMDCT512(float[] coeffs) {
                // Pre-IFFT
                for (int i = 0; i < intermediate.Length; i++) {
                    intermediate[i] = new Complex(coeffs[IMDCTSize / 2 - 1 - 2 * i], coeffs[2 * i]) * x512[i];
                }

                // IFFT
                intermediate.InPlaceIFFTUnscaled(cache512);

                // Post-IFFT
                intermediate.Convolve(x512);

                // Windowing and de-interleaving
                for (int i = 0; i < IMDCTSize / 8; i++) {
                    const int n8 = IMDCTSize / 8,
                        n4 = IMDCTSize / 4,
                        n2 = IMDCTSize / 2;
                    output[2 * i] = (float)-intermediate[n8 + i].Imaginary * window[2 * i];
                    output[2 * i + 1] = (float)intermediate[n8 - 1 - i].Real * window[2 * i + 1];
                    output[n4 + 2 * i] = (float)-intermediate[i].Real * window[n4 + 2 * i];
                    output[n4 + 1 + 2 * i] = (float)intermediate[n4 - 1 - i].Imaginary * window[n4 + 1 + 2 * i];
                    output[n2 + 2 * i] = (float)-intermediate[n8 + i].Real * window[n2 - 1 - 2 * i];
                    output[n2 + 1 + 2 * i] = (float)intermediate[n8 - 1 - i].Imaginary * window[n2 - 2 - 2 * i];
                    output[3 * n4 + 2 * i] = (float)intermediate[i].Imaginary * window[n4 - 1 - 2 * i];
                    output[3 * n4 + 1 + 2 * i] = (float)-intermediate[n4 - 1 - i].Real * window[n4 - 2 - 2 * i];
                }

                // Overlap-and-add
                for (int i = 0; i < delay.Length; i++) {
                    coeffs[i] = 2 * (output[i] + delay[i]);
                }
                Array.Copy(output, delay.Length, delay, 0, delay.Length);
            }

            /// <summary>
            /// Performs a 256-sample inverse modified discrete cosine transform in-place.
            /// </summary>
            public void IMDCT256(float[] coeffs) {
                for (int i = 0; i < coeffSplit1.Length; i++) {
                    coeffSplit1[i] = coeffs[2 * i];
                    coeffSplit2[i] = coeffs[2 * i + 1];
                }

                // Pre-IFFT
                for (int i = 0; i < intermediate1.Length; i++) {
                    intermediate1[i] = new Complex(coeffSplit1[IMDCTSize / 4 - 1 - 2 * i], coeffs[2 * i]) * x256[i];
                }
                for (int i = 0; i < intermediate2.Length; i++) {
                    intermediate2[i] = new Complex(coeffSplit2[IMDCTSize / 4 - 1 - 2 * i], coeffs[2 * i]) * x256[i];
                }

                // IFFT
                intermediate1.InPlaceIFFTUnscaled(cache256);
                intermediate2.InPlaceIFFTUnscaled(cache256);

                // Post-IFFT
                intermediate1.Convolve(x256);
                intermediate2.Convolve(x256);

                // Windowing and de-interleaving
                for (int i = 0; i < IMDCTSize / 8; i++) {
                    const int n8 = IMDCTSize / 8,
                        n4 = IMDCTSize / 4,
                        n2 = IMDCTSize / 2;
                    output[2 * i] = -intermediate1[i].Imaginary * window[2 * i];
                    output[2 * i + 1] = intermediate1[n8 - 1 - i].Real * window[2 * i + 1];
                    output[n4 + 2 * i] = -intermediate1[i].Real * window[n4 + 2 * i];
                    output[n4 + 1 + 2 * i] = intermediate1[n8 - 1 - i].Imaginary * window[n4 + 1 + 2 * i];
                    output[n2 + 2 * i] = -intermediate2[i].Real * window[n2 - 1 - 2 * i];
                    output[n2 + 1 + 2 * i] = intermediate2[n8 - 1 - i].Imaginary * window[n2 - 2 - 2 * i];
                    output[3 * n4 + 2 * i] = intermediate2[i].Imaginary * window[n4 - 1 - 2 * i];
                    output[3 * n4 + 1 + 2 * i] = -intermediate2[n8 - 1 - i].Real * window[n4 - 2 - 2 * i];
                }

                // Overlap-and-add
                for (int i = 0; i < delay.Length; i++) {
                    coeffs[i] = 2 * (output[i] + delay[i]);
                }
                Array.Copy(output, delay.Length, delay, 0, delay.Length);
            }

            /// <summary>
            /// Perform the actual work of <see cref="ReadTransformCoeffs(BitExtractor, float[], int, int)"/>.
            /// </summary>
            /// <remarks>Skipping this step is a huge performance gain in AC-3 to AC-3 transcoding.
            /// Values are 24-bit and signed, these can be mapped to floats without loss.</remarks>
            void DecodeTransformCoeffs(BitExtractor extractor, float[] target, int start, int end) {
                int bap1Pos = host.bap1Pos;
                int bap2Pos = host.bap2Pos;
                int bap4Pos = host.bap4Pos;
                int[] bap1Next = host.bap1Next;
                int[] bap2Next = host.bap2Next;
                int[] bap4Next = host.bap4Next;

                for (int bin = start; bin < end; ++bin) {
                    switch (bap[bin]) {
                        case 1:
                            if (++bap1Pos == 3) {
                                bap1Next = bap1[extractor.Read(bap1Bits)];
                                bap1Pos = 0;
                            }
                            target[bin] = (bap1Next[bap1Pos] >> exponents[bin]) * BitConversions.fromInt24;
                            break;
                        case 2:
                            if (++bap2Pos == 3) {
                                bap2Next = bap2[extractor.Read(bap2Bits)];
                                bap2Pos = 0;
                            }
                            target[bin] = (bap2Next[bap2Pos] >> exponents[bin]) * BitConversions.fromInt24;
                            break;
                        case 3:
                            target[bin] = (bap3[extractor.Read(bap3Bits)] >> exponents[bin]) * BitConversions.fromInt24;
                            break;
                        case 4:
                            if (++bap4Pos == 2) {
                                bap4Next = bap4[extractor.Read(bap4Bits)];
                                bap4Pos = 0;
                            }
                            target[bin] = (bap4Next[bap4Pos] >> exponents[bin]) * BitConversions.fromInt24;
                            break;
                        case 5:
                            target[bin] = (bap5[extractor.Read(bap5Bits)] >> exponents[bin]) * BitConversions.fromInt24;
                            break;
                        default: // Asymmetric quantization
                            target[bin] = ((extractor.Read(bitsToRead[bap[bin]]) << (32 - bitsToRead[bap[bin]])) >> exponents[bin])
                                * BitConversions.fromInt32;
                            break;
                    }
                }

                host.bap1Pos = bap1Pos;
                host.bap2Pos = bap2Pos;
                host.bap4Pos = bap4Pos;
                host.bap1Next = bap1Next;
                host.bap2Next = bap2Next;
                host.bap4Next = bap4Next;
            }
        }
    }
}