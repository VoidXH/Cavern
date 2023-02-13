using System;

using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a PCM stream to a quadrature mirror filter bank and back.
    /// </summary>
    partial class QuadratureMirrorFilterBank {
        /// <summary>
        /// Forward transformation cache of rotation cosines and sines.
        /// </summary>
        static readonly (float[] real, float[] imaginary)[] forwardCache = new (float[], float[])[subbands];

        /// <summary>
        /// Inverse transformation cache of rotation cosines and sines.
        /// </summary>
        static readonly (float[] real, float[] imaginary)[] inverseCache = new (float[], float[])[subbands];

        /// <summary>
        /// Input sample cache for forward transformations.
        /// </summary>
        readonly float[] inputStreamForward = new float[coeffs.Length];

        /// <summary>
        /// Input sample cache for inverse transformations.
        /// </summary>
        readonly float[] inputStreamInverse = new float[coeffs.Length * 2];

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
        readonly (float[] real, float[] imaginary) outCache;

        /// <summary>
        /// Converts a PCM stream to a quadrature mirror filter bank and back.
        /// </summary>
        public QuadratureMirrorFilterBank() {
            window = new float[coeffs.Length];
            grouping = new float[doubleLength];
            outCache = (new float[subbands], new float[subbands]);

            if (forwardCache[0].real == null) {
                for (int sb = 0; sb < subbands; ++sb) {
                    forwardCache[sb] = (new float[doubleLength], new float[doubleLength]);
                    inverseCache[sb] = (new float[doubleLength], new float[doubleLength]);
                    for (int j = 0; j < doubleLength; ++j) {
                        float exp = MathF.PI * (sb + .5f) * (j - .5f) * subbandDiv;
                        forwardCache[sb].real[j] = MathF.Cos(exp);
                        forwardCache[sb].imaginary[j] = MathF.Sin(exp);
                        exp = MathF.PI * (sb + .5f) * (j - doubleLength + .5f) * subbandDiv;
                        inverseCache[sb].real[j] = MathF.Cos(exp) * subbandDiv;
                        inverseCache[sb].imaginary[j] = MathF.Sin(exp) * subbandDiv;
                    }
                }
            }
        }

        /// <summary>
        /// Transform a timeslot of real <see cref="subbands"/> to QMFB.
        /// </summary>
        public unsafe (float[] real, float[] imaginary) ProcessForward(float* input) {
            Array.Copy(inputStreamForward, 0, inputStreamForward, subbands, coeffs.Length - subbands);
            fixed (float* pInputStream = inputStreamForward, pCoeffs = coeffs) {
                float* inputPos = input + subbands - 1,
                    inputStreamPos = pInputStream,
                    end = inputStreamPos + subbands;
                while (inputStreamPos != end) {
                    *inputStreamPos++ = *inputPos--;
                }

                QMath.MultiplyAndSet(pInputStream, pCoeffs, grouping, doubleLength);
                for (int sample = doubleLength; sample < window.Length; sample += doubleLength) {
                    QMath.MultiplyAndAdd(pInputStream + sample, pCoeffs + sample, grouping, doubleLength);
                }
            }

            for (int sb = 0; sb < subbands; sb++) {
                fixed (float* real = forwardCache[sb].real, imaginary = forwardCache[sb].imaginary, pGrouping = grouping) {
                    outCache.real[sb] = QMath.MultiplyAndAdd(real, pGrouping, doubleLength);
                    outCache.imaginary[sb] = QMath.MultiplyAndAdd(imaginary, pGrouping, doubleLength);
                }
            }
            return outCache;
        }

        /// <summary>
        /// Convert a timeslot of QMFB <see cref="subbands"/> to PCM samples.
        /// </summary>
        public unsafe void ProcessInverse((float[] real, float[] imaginary) input, float[] output) {
            Array.Copy(inputStreamInverse, 0, inputStreamInverse, doubleLength, inputStreamInverse.Length - doubleLength);

            fixed (float* cacheReal = inverseCache[0].real, cacheImaginary = inverseCache[0].imaginary) {
                QMath.MultiplyAndSet(cacheReal, input.real[0], cacheImaginary, -input.imaginary[0], inputStreamInverse, doubleLength);
            }
            for (int sb = 1; sb < subbands; ++sb) {
                fixed (float* cacheReal = inverseCache[sb].real, cacheImaginary = inverseCache[sb].imaginary) {
                    QMath.MultiplyAndAdd(cacheReal, input.real[sb], cacheImaginary, -input.imaginary[sb], inputStreamInverse, doubleLength);
                }
            }

            fixed (float* pInputStreamInverse = inputStreamInverse, pCoeffs = coeffs) {
                QMath.MultiplyAndSet(pInputStreamInverse, pCoeffs,
                        pInputStreamInverse + subbands * 3, pCoeffs + subbands, output, subbands);
                for (int j = 1, end = coeffs.Length / doubleLength; j < end; j++) {
                    int timeSlot = subbands * 4 * j;
                    int coeffSlot = doubleLength * j;
                    int timePair = timeSlot + subbands * 3;
                    int coeffPair = coeffSlot + subbands;
                    QMath.MultiplyAndAdd(pInputStreamInverse + timeSlot, pCoeffs + coeffSlot,
                        pInputStreamInverse + timePair, pCoeffs + coeffPair, output, subbands);
                }
            }
        }
    }
}