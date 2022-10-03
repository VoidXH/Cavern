using System;
using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        partial class Allocation {
            // TODO: these shouldn't be static, just once per transcoder
            /// <summary>
            /// Index of the next mantissa in <see cref="bap1Next"/>.
            /// </summary>
            static int bap1Pos;

            /// <summary>
            /// Index of the next mantissa in <see cref="bap2Next"/>.
            /// </summary>
            static int bap2Pos;

            /// <summary>
            /// Index of the next mantissa in <see cref="bap4Next"/>.
            /// </summary>
            static int bap4Pos;

            /// <summary>
            /// Next mantissa values in case the bap is 1.
            /// </summary>
            static float[] bap1Next;

            /// <summary>
            /// Next mantissa values in case the bap is 2.
            /// </summary>
            static float[] bap2Next;

            /// <summary>
            /// Next mantissa values in case the bap is 4.
            /// </summary>
            static float[] bap4Next;

            /// <summary>
            /// A copy of the mantissa stream to be able to quickly transcode the block without re-encoding.
            /// </summary>
            byte[] rawMantissa = new byte[0];

            /// <summary>
            /// Length of the content in <see cref="rawMantissa"/> in bits.
            /// </summary>
            int mantissaBits;

            public static void ResetBlock() {
                bap1Pos = 2;
                bap2Pos = 2;
                bap4Pos = 1;
            }

            /// <summary>
            /// Read the encoded mantissa values and convert them to transform coeffs into the <paramref name="target"/> array.
            /// </summary>
            public void ReadTransformCoeffs(BitExtractor extractor, float[] target, int start, int end) {
                mantissaBits = 0; // Additionally, keep raw data in a byte array to make transcoding possible
                int bapReads = 0; // 4 bytes for the first 4 baps, counting their read count efficiently
                for (int bin = start; bin < end; ++bin) {
                    if (bap[bin] != 0 && bap[bin] < 5) {
                        bapReads += 1 << ((bap[bin] - 1) * 8);
                    } else {
                        mantissaBits += bitsToRead[bap[bin]];
                    }
                }
                mantissaBits +=
                    (bap1Pos + (bapReads & 0xFF)) / 3 * bitsToRead[1] +
                    (bap2Pos + ((bapReads >> 8) & 0xFF)) / 3 * bitsToRead[2] +
                    ((bapReads >> 16) & 0xFF) * bitsToRead[3] +
                    ((bap4Pos + (bapReads >> 24)) / 2) * bitsToRead[4];
                extractor.ReadBitsInto(ref rawMantissa, mantissaBits);
                DecodeTransformCoeffs(new BitExtractor(rawMantissa), target, start, end);
            }



            /// <summary>
            /// Performs a 512-sample inverse modified discrete cosine transform in-place.
            /// </summary>
            public void IMDCT512(float[] coeffs) {
                // Pre-IFFT
                for (int i = 0; i < Z.Length; i++) {
                    Z[i] = new Complex(coeffs[IMDCTSize / 2 - 1 - 2 * i], coeffs[2 * i]) * x512[i];
                }

                // IFFT
                for (int i = 0; i < y.Length; i++) {
                    Complex result = 0;
                    for (int j = 0; j < y.Length; j++) {
                        float phi = 8 * MathF.PI / IMDCTSize * i * j;
                        result += new Complex(Z[i].Real * MathF.Cos(phi), Z[i].Imaginary * MathF.Sin(phi));
                    }
                    y[i] = result;
                }

                // Post-IFFT
                for (int i = 0; i < y.Length; i++) {
                    y[i] *= x512[i];
                }

                // Windowing and de-interleaving
                for (int i = 0; i < IMDCTSize / 8; i++) {
                    x[2 * i] = (float)-y[IMDCTSize / 8 + i].Imaginary * IMDCTWindow[2 * i];
                    x[2 * i + 1] = (float)y[IMDCTSize / 8 - i - 1].Real * IMDCTWindow[2 * i + 1];
                    x[IMDCTSize / 4 + 2 * i] = (float)-y[i].Real * IMDCTWindow[IMDCTSize / 4 + 2 * i];
                    x[IMDCTSize / 4 + 2 * i + 1] = (float)y[IMDCTSize / 4 - 1 - i].Imaginary * IMDCTWindow[IMDCTSize / 4 + 1 + 2 * i];
                    x[IMDCTSize / 2 + 2 * i] = (float)-y[IMDCTSize / 8 + i].Real * IMDCTWindow[IMDCTSize / 2 - 1 - 2 * i];
                    x[IMDCTSize / 2 + 2 * i + 1] = (float)y[IMDCTSize / 8 - 1 - i].Imaginary * IMDCTWindow[IMDCTSize / 2 - 2 - 2 * i];
                    x[3 * IMDCTSize / 4 + 2 * i] = (float)y[i].Imaginary * IMDCTWindow[IMDCTSize / 4 - 1 - 2 * i];
                    x[3 * IMDCTSize / 4 + 2 * i + 1] = (float)-y[IMDCTSize / 4 - 1 - i].Real * IMDCTWindow[IMDCTSize / 4 - 2 - 2 * i];
                }

                // Overlap-and-add
                for (int i = 0; i < delay.Length; i++) {
                    coeffs[i] = 2 * (x[i] + delay[i]);
                }
                Array.Copy(x, delay.Length, delay, 0, delay.Length);
            }

            /// <summary>
            /// Performs a 256-sample inverse modified discrete cosine transform in-place.
            /// </summary>
            public void IMDCT256(float[] coeffs) {
                throw new UnsupportedFeatureException("blksw");
            }

            /// <summary>
            /// Perform the actual work of <see cref="ReadTransformCoeffs(BitExtractor, float[], int, int)"/>.
            /// </summary>
            /// <remarks>Skipping this step is a huge performance gain in AC-3 to AC-3 transcoding.
            /// Values are 24-bit and signed, these can be mapped to floats without loss.</remarks>
            void DecodeTransformCoeffs(BitExtractor extractor, float[] target, int start, int end) {
                for (int bin = start; bin < end; ++bin) {
                    switch (bap[bin]) {
                        case 1:
                            if (++bap1Pos == 3) {
                                bap1Next = bap1[extractor.Read(bitsToRead[1])];
                                bap1Pos = 0;
                            }
                            target[bin] = bap1Next[bap1Pos];
                            break;
                        case 2:
                            if (++bap2Pos == 3) {
                                bap2Next = bap2[extractor.Read(bitsToRead[2])];
                                bap2Pos = 0;
                            }
                            target[bin] = bap2Next[bap2Pos];
                            break;
                        case 3:
                            target[bin] = bap3[extractor.Read(bitsToRead[3])];
                            break;
                        case 4:
                            if (++bap4Pos == 2) {
                                bap4Next = bap4[extractor.Read(bitsToRead[4])];
                                bap4Pos = 0;
                            }
                            target[bin] = bap4Next[bap4Pos];
                            break;
                        case 5:
                            target[bin] = bap5[extractor.Read(bitsToRead[5])];
                            break;
                        default: // Asymmetric quantization
                            target[bin] = ((extractor.Read(bitsToRead[bap[bin]]) << (32 - bitsToRead[bap[bin]])) >> (8 + exp[bin]))
                                * BitConversions.fromInt24;
                            break;
                    }
                }
            }
        }
    }
}