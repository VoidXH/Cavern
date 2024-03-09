using System;

using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        partial class Allocation {
            /// <summary>
            /// Reference exponent for decoding others with offsets (absexp).
            /// </summary>
            public int absoluteExponent;

            /// <summary>
            /// Grouped exponents read from the bitstream (gexp/exps). A 7-bit encoded exponent data contains 3 consecutive exponents.
            /// </summary>
            /// <remarks>Handling of this array differs from the reference code,
            /// the first element is extracted to <see cref="absoluteExponent"/>.</remarks>
            public readonly int[] groupedExponents;

            /// <summary>
            /// Fully decoded, final exponents, used for shifting mantissas to place (exp).
            /// </summary>
            public readonly int[] exponents;

            /// <summary>
            /// Exponents mapped for power spectral density.
            /// </summary>
            public readonly int[] psd;

            /// <summary>
            /// PSD summed for each masked band (bndpsd).
            /// </summary>
            public readonly int[] integratedPSD;

            public readonly int[] excite;
            public readonly int[] mask;
            public readonly byte[] bap;

            /// <summary>
            /// Precalculated values for the 512-sample IMDCT's IFFT.
            /// </summary>
            static readonly FFTCache cache512 = new FFTCache(IMDCTSize / 4);

            /// <summary>
            /// Precalculated values for the 256-sample IMDCT's IFFT.
            /// </summary>
            static readonly FFTCache cache256 = new FFTCache(IMDCTSize / 8);

            /// <summary>
            /// Grouped mantissas for bap = 1 (3-level) quantization.
            /// </summary>
            static readonly int[][] bap1 = GenerateGroupedQuantization(3, 3, 5);

            /// <summary>
            /// Grouped mantissas for bap = 2 (5-level) quantization.
            /// </summary>
            static readonly int[][] bap2 = GenerateGroupedQuantization(5, 3, 7);

            /// <summary>
            /// Mantissas for bap = 3 (7-level) quantization.
            /// </summary>
            static readonly int[] bap3 = GenerateQuantization(7);

            /// <summary>
            /// Grouped mantissas for bap = 4 (11-level) quantization.
            /// </summary>
            static readonly int[][] bap4 = GenerateGroupedQuantization(11, 2, 7);

            /// <summary>
            /// Mantissas for bap = 5 (15-level) quantization.
            /// </summary>
            static readonly int[] bap5 = GenerateQuantization(15);

            /// <summary>
            /// Complex multiplication cache for 512-sample IMDCT.
            /// </summary>
            static readonly Complex[] x512 = CreateCoefficients(128);

            /// <summary>
            /// Complex multiplication cache for 256-sample IMDCT.
            /// </summary>
            static readonly Complex[] x256 = CreateCoefficients(64);

            /// <summary>
            /// Intermediate IMDCT array for 512-sample IMDCT.
            /// </summary>
            readonly Complex[] intermediate;

            /// <summary>
            /// Intermediate IMDCT array for even 256-sample IMDCT.
            /// </summary>
            readonly Complex[] intermediate1;

            /// <summary>
            /// Intermediate IMDCT array for odd 256-sample IMDCT.
            /// </summary>
            readonly Complex[] intermediate2;

            /// <summary>
            /// Windowed time-domain samples after IMDCT.
            /// </summary>
            readonly float[] output;

            /// <summary>
            /// Cache array for overlap-and-add.
            /// </summary>
            readonly float[] delay;

            /// <summary>
            /// Even coefficients in 256-sample IMDCT mode.
            /// </summary>
            readonly float[] coeffSplit1;

            /// <summary>
            /// Odd coefficients in 256-sample IMDCT mode.
            /// </summary>
            readonly float[] coeffSplit2;

            /// <summary>
            /// The body for which the allocation is read.
            /// </summary>
            readonly EnhancedAC3Body host;

            /// <summary>
            /// Contains and decodes exponent and mantissa data for a single channels.
            /// </summary>
            public Allocation(EnhancedAC3Body host, int maxLength) {
                this.host = host;

                groupedExponents = new int[maxLength];
                exponents = new int[maxLength];
                psd = new int[maxLength];
                integratedPSD = new int[maxLength];
                excite = new int[maxLength];
                mask = new int[maxLength];
                bap = new byte[maxLength];

                intermediate = new Complex[IMDCTSize / 4];
                output = new float[IMDCTSize];
                delay = new float[IMDCTSize / 2];

                coeffSplit1 = new float[IMDCTSize / 4];
                coeffSplit2 = new float[IMDCTSize / 4];
                intermediate1 = new Complex[IMDCTSize / 8];
                intermediate2 = new Complex[IMDCTSize / 8];
            }

            /// <summary>
            /// Create the coefficients for an IMDCT transform.
            /// </summary>
            static Complex[] CreateCoefficients(int imdctSize) {
                Complex[] result = new Complex[imdctSize];
                float mul = 2 * MathF.PI / (imdctSize << 5);
                for (int i = 0; i < imdctSize; i++) {
                    float phi = mul * (8 * i + 1);
                    result[i] = new Complex(-MathF.Cos(phi), -MathF.Sin(phi));
                }
                return result;
            }

            /// <summary>
            /// Generate the quantized values for a given symmetric quantizer (bap 1 to 5, by number of levels).
            /// </summary>
            /// <param name="levels">Number of quantized values</param>
            static int[] GenerateQuantization(int levels) {
                int[] result = new int[levels + 1];
                int numerator = -1 - levels;
                for (int i = 0; i < levels; i++) {
                    result[i] = BitConversions.int24Max * (numerator += 2) / levels;
                }
                return result;
            }

            /// <summary>
            /// Generate the groups of quantized values for a given grouped symmetric quantizer (bap 1, 2, and 5).
            /// </summary>
            /// <param name="levels">Number of quantized values</param>
            /// <param name="groups">Number of values grouped per written mantissa group</param>
            /// <param name="groupBits">Bits used by a mantissa group</param>
            static int[][] GenerateGroupedQuantization(int levels, int groups, int groupBits) {
                int[] source = GenerateQuantization(levels);
                int[][] result = new int[1 << groupBits][];
                for (int i = 0; i < result.Length; i++) {
                    result[i] = new int[groups];
                    int groupCode = i;
                    for (int j = groups - 1; j >= 0; j--) {
                        result[i][j] = source[groupCode % levels];
                        groupCode /= levels;
                    }
                }
                return result;
            }

            /// <summary>
            /// Maximum IMDCT transformation size.
            /// </summary>
            const int IMDCTSize = 512;

            /// <summary>
            /// Windowing function for the IMDCT transformations.
            /// </summary>
            static readonly float[] window = {
                0.00014f, 0.00024f, 0.00037f, 0.00051f, 0.00067f, 0.00086f, 0.00107f, 0.00130f, 0.00157f, 0.00187f,
                0.00220f, 0.00256f, 0.00297f, 0.00341f, 0.00390f, 0.00443f, 0.00501f, 0.00564f, 0.00632f, 0.00706f,
                0.00785f, 0.00871f, 0.00962f, 0.01061f, 0.01166f, 0.01279f, 0.01399f, 0.01526f, 0.01662f, 0.01806f,
                0.01959f, 0.02121f, 0.02292f, 0.02472f, 0.02662f, 0.02863f, 0.03073f, 0.03294f, 0.03527f, 0.03770f,
                0.04025f, 0.04292f, 0.04571f, 0.04862f, 0.05165f, 0.05481f, 0.05810f, 0.06153f, 0.06508f, 0.06878f,
                0.07261f, 0.07658f, 0.08069f, 0.08495f, 0.08935f, 0.09389f, 0.09859f, 0.10343f, 0.10842f, 0.11356f,
                0.11885f, 0.12429f, 0.12988f, 0.13563f, 0.14152f, 0.14757f, 0.15376f, 0.16011f, 0.16661f, 0.17325f,
                0.18005f, 0.18699f, 0.19407f, 0.20130f, 0.20867f, 0.21618f, 0.22382f, 0.23161f, 0.23952f, 0.24757f,
                0.25574f, 0.26404f, 0.27246f, 0.28100f, 0.28965f, 0.29841f, 0.30729f, 0.31626f, 0.32533f, 0.33450f,
                0.34376f, 0.35311f, 0.36253f, 0.37204f, 0.38161f, 0.39126f, 0.40096f, 0.41072f, 0.42054f, 0.43040f,
                0.44030f, 0.45023f, 0.46020f, 0.47019f, 0.48020f, 0.49022f, 0.50025f, 0.51028f, 0.52031f, 0.53033f,
                0.54033f, 0.55031f, 0.56026f, 0.57019f, 0.58007f, 0.58991f, 0.59970f, 0.60944f, 0.61912f, 0.62873f,
                0.63827f, 0.64774f, 0.65713f, 0.66643f, 0.67564f, 0.68476f, 0.69377f, 0.70269f, 0.71150f, 0.72019f,
                0.72877f, 0.73723f, 0.74557f, 0.75378f, 0.76186f, 0.76981f, 0.77762f, 0.78530f, 0.79283f, 0.80022f,
                0.80747f, 0.81457f, 0.82151f, 0.82831f, 0.83496f, 0.84145f, 0.84779f, 0.85398f, 0.86001f, 0.86588f,
                0.87160f, 0.87716f, 0.88257f, 0.88782f, 0.89291f, 0.89785f, 0.90264f, 0.90728f, 0.91176f, 0.91610f,
                0.92028f, 0.92432f, 0.92822f, 0.93197f, 0.93558f, 0.93906f, 0.94240f, 0.94560f, 0.94867f, 0.95162f,
                0.95444f, 0.95713f, 0.95971f, 0.96217f, 0.96451f, 0.96674f, 0.96887f, 0.97089f, 0.97281f, 0.97463f,
                0.97635f, 0.97799f, 0.97953f, 0.98099f, 0.98236f, 0.98366f, 0.98488f, 0.98602f, 0.98710f, 0.98811f,
                0.98905f, 0.98994f, 0.99076f, 0.99153f, 0.99225f, 0.99291f, 0.99353f, 0.99411f, 0.99464f, 0.99513f,
                0.99558f, 0.99600f, 0.99639f, 0.99674f, 0.99706f, 0.99736f, 0.99763f, 0.99788f, 0.99811f, 0.99831f,
                0.99850f, 0.99867f, 0.99882f, 0.99895f, 0.99908f, 0.99919f, 0.99929f, 0.99938f, 0.99946f, 0.99953f,
                0.99959f, 0.99965f, 0.99969f, 0.99974f, 0.99978f, 0.99981f, 0.99984f, 0.99986f, 0.99988f, 0.99990f,
                0.99992f, 0.99993f, 0.99994f, 0.99995f, 0.99996f, 0.99997f, 0.99998f, 0.99998f, 0.99998f, 0.99999f,
                0.99999f, 0.99999f, 0.99999f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            };
        }
    }
}