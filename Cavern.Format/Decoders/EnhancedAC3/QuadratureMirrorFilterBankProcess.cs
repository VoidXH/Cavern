using System;
using System.Numerics;

using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a PCM stream to a quadrature mirror filter bank and back.
    /// </summary>
    partial class QuadratureMirrorFilterBank {
        /// <summary>
        /// Forward transformation cache of rotation cosines and sines. Vectors are used for their SIMD properties.
        /// </summary>
        static Vector2[][] forwardCache;

        /// <summary>
        /// Inverse transformation cache of rotation cosines and sines. Vectors are used for their SIMD properties.
        /// </summary>
        static Vector2[][] inverseCache;

        /// <summary>
        /// Input sample cache for forward transformations.
        /// </summary>
        readonly float[] inputStreamForward;

        /// <summary>
        /// Input sample cache for inverse transformations.
        /// </summary>
        readonly float[] inputStreamInverse;

        /// <summary>
        /// Processor window cache.
        /// </summary>
        readonly float[] window;

        /// <summary>
        /// Summation of <see cref="window"/> groups.
        /// </summary>
        readonly float[] grouping;

        /// <summary>
        /// Output cache.
        /// </summary>
        readonly Vector2[] outCache;

        /// <summary>
        /// Converts a PCM stream to a quadrature mirror filter bank and back.
        /// </summary>
        public QuadratureMirrorFilterBank() {
            int doubleLength = subbands * 2;
            inputStreamForward = new float[coeffs.Length];
            inputStreamInverse = new float[coeffs.Length * 2];
            window = new float[coeffs.Length];
            grouping = new float[doubleLength];
            outCache = new Vector2[subbands];

            if (forwardCache == null) {
                forwardCache = new Vector2[subbands][];
                inverseCache = new Vector2[subbands][];
                for (int sb = 0; sb < subbands; ++sb) {
                    forwardCache[sb] = new Vector2[doubleLength];
                    inverseCache[sb] = new Vector2[doubleLength];
                    for (int j = 0; j < doubleLength; ++j) {
                        float exp = MathF.PI * (sb + .5f) * (j - .5f) * subbandDiv;
                        forwardCache[sb][j] = new Vector2(MathF.Cos(exp), MathF.Sin(exp));
                        exp = MathF.PI * (sb + .5f) * (j - doubleLength + .5f) * subbandDiv;
                        inverseCache[sb][j] = new Vector2(MathF.Cos(exp), MathF.Sin(exp)) * subbandDiv;
                    }
                }
            }
        }

        /// <summary>
        /// Convert a timeslot of real <see cref="subbands"/> to QMFB.
        /// </summary>
        public Vector2[] ProcessForward(float[] input) {
            Array.Copy(inputStreamForward, 0, inputStreamForward, subbands, coeffs.Length - subbands);
            for (int sample = 0; sample < subbands; ++sample) {
                inputStreamForward[sample] = input[subbands - sample - 1];
            }

            int doubleLength = subbands * 2;
            Array.Clear(grouping, 0, doubleLength);
            for (int sample = 0; sample < window.Length; ++sample) {
                grouping[sample & groupingMask] += inputStreamForward[sample] * coeffs[sample];
            }

            outCache.Clear();
            for (int sb = 0; sb < subbands; ++sb) {
                Vector2 result = new Vector2();
                Vector2[] cache = forwardCache[sb];
                for (int j = 0; j < doubleLength; ++j) {
                    result += cache[j] * grouping[j];
                }
                outCache[sb] = result;
            }
            return outCache;
        }

        /// <summary>
        /// Convert a timeslot of QMFB <see cref="subbands"/> to PCM samples.
        /// </summary>
        public void ProcessInverse(Vector2[] input, float[] output) {
            int doubleLength = subbands * 2;

            Array.Copy(inputStreamInverse, 0, inputStreamInverse, doubleLength, inputStreamInverse.Length - doubleLength);
            Array.Clear(inputStreamInverse, 0, doubleLength);
            for (int sb = 0; sb < subbands; ++sb) {
                Vector2[] cache = inverseCache[sb];
                Vector2 mul = input[sb];
                for (int j = 0; j < doubleLength; ++j) {
                    Vector2 result = cache[j] * mul;
                    inputStreamInverse[j] += result.X - result.Y;
                }
            }

            Array.Clear(output, 0, subbands);
            for (int j = 0, end = coeffs.Length / doubleLength; j < end; ++j) {
                int timeSlot = subbands * 4 * j;
                int coeffSlot = doubleLength * j;
                int pair = timeSlot + subbands * 3;
                int coeffPair = coeffSlot + subbands;
                for (int sb = 0; sb < subbands; ++sb) {
                    output[sb] += inputStreamInverse[timeSlot + sb] * coeffs[coeffSlot + sb] +
                        inputStreamInverse[pair + sb] * coeffs[coeffPair + sb];
                }
            }
        }
    }
}