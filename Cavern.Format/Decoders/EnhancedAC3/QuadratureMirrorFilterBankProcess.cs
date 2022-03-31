using Cavern.Utilities;
using System;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a PCM stream to a quadrature mirror filter bank and back.
    /// </summary>
    partial class QuadratureMirrorFilterBank {
        /// <summary>
        /// 1 / <see cref="subbands"/>.
        /// </summary>
        const float subbandDiv = 1f / subbands;

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
        readonly Complex[] outCache;

        /// <summary>
        /// Forward transformation cache of rotation sines.
        /// </summary>
        readonly float[][] sinCacheFwd;

        /// <summary>
        /// Forward transformation cache of rotation cosines.
        /// </summary>
        readonly float[][] cosCacheFwd;

        /// <summary>
        /// Inverse transformation cache of rotation sines.
        /// </summary>
        readonly float[][] sinCacheInv;

        /// <summary>
        /// Inverse transformation cache of rotation cosines.
        /// </summary>
        readonly float[][] cosCacheInv;

        /// <summary>
        /// Converts a PCM stream to a quadrature mirror filter bank and back.
        /// </summary>
        public QuadratureMirrorFilterBank() {
            int doubleLength = subbands * 2;
            inputStreamForward = new float[coeffs.Length];
            inputStreamInverse = new float[coeffs.Length * 2];
            window = new float[coeffs.Length];
            grouping = new float[doubleLength];
            outCache = new Complex[subbands];

            sinCacheFwd = new float[subbands][];
            cosCacheFwd = new float[subbands][];
            sinCacheInv = new float[subbands][];
            cosCacheInv = new float[subbands][];
            for (int sb = 0; sb < subbands; ++sb) {
                sinCacheFwd[sb] = new float[doubleLength];
                cosCacheFwd[sb] = new float[doubleLength];
                sinCacheInv[sb] = new float[doubleLength];
                cosCacheInv[sb] = new float[doubleLength];
                for (int j = 0; j < doubleLength; ++j) {
                    float exp = MathF.PI * (sb + .5f) * (j - .5f) * subbandDiv;
                    sinCacheFwd[sb][j] = MathF.Sin(exp);
                    cosCacheFwd[sb][j] = MathF.Cos(exp);
                    exp = MathF.PI / (doubleLength * 2) * (2 * sb + 1) * (2 * j - doubleLength - 1);
                    sinCacheInv[sb][j] = MathF.Sin(exp);
                    cosCacheInv[sb][j] = MathF.Cos(exp);
                }
            }
        }

        /// <summary>
        /// Convert a timeslot of real <see cref="subbands"/> to QMFB.
        /// </summary>
        public Complex[] ProcessForward(float[] input) {
            Array.Copy(inputStreamForward, 0, inputStreamForward, subbands, coeffs.Length - subbands);
            for (int sample = 0; sample < subbands; ++sample)
                inputStreamForward[sample] = input[subbands - sample - 1];

            for (int sample = 0; sample < window.Length; ++sample)
                window[sample] = inputStreamForward[sample] * coeffs[sample];

            int doubleLength = subbands * 2;
            for (int j = 0, end = coeffs.Length / doubleLength; j < doubleLength; ++j) {
                float groupingValue = 0;
                for (int k = 0; k < end; ++k)
                    groupingValue += window[j + k * doubleLength];
                grouping[j] = groupingValue;
            }

            Array.Clear(outCache, 0, outCache.Length);
            for (int sb = 0; sb < subbands; ++sb)
                for (int j = 0; j < doubleLength; ++j)
                    outCache[sb] += new Complex(grouping[j] * cosCacheFwd[sb][j], grouping[j] * sinCacheFwd[sb][j]);
            return outCache;
        }

        /// <summary>
        /// Convert a timeslot of QMFB <see cref="subbands"/> to PCM samples.
        /// </summary>
        public void ProcessInverse(Complex[] input, float[] output) {
            int doubleLength = subbands * 2;
            int quadrupleLength = subbands * 4;

            Array.Copy(inputStreamInverse, 0, inputStreamInverse, doubleLength, inputStreamInverse.Length - doubleLength);
            for (int j = 0; j < doubleLength; ++j) {
                inputStreamInverse[j] = 0;
                for (int sb = 0; sb < subbands; ++sb)
                    inputStreamInverse[j] += (input[sb].Real * cosCacheInv[sb][j] -
                        input[sb].Imaginary * sinCacheInv[sb][j]) * subbandDiv;
            }

            for (int j = 0, end = coeffs.Length / doubleLength; j < end; ++j) {
                int timeSlot = quadrupleLength * j;
                int pair = timeSlot + subbands * 3;
                for (int sb = 0; sb < subbands; ++sb) {
                    window[doubleLength * j + sb] = inputStreamInverse[timeSlot + sb];
                    window[doubleLength * j + subbands + sb] = inputStreamInverse[pair + sb];
                }
            }

            for (int j = 0; j < window.Length; ++j)
                window[j] *= coeffs[j];

            for (int ts = 0; ts < subbands; ++ts) {
                float outSample = 0;
                for (int j = 0, end = coeffs.Length / subbands; j < end; ++j)
                    outSample += window[subbands * j + ts];
                output[ts] = outSample;
            }
        }
    }
}