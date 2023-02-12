using System;

using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    // Non-vectorized versions of QMFB functions, as they run faster in Mono.
    partial class QuadratureMirrorFilterBank {
        /// <summary>
        /// Transform a timeslot of real <see cref="subbands"/> to QMFB.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public unsafe (float[] real, float[] imaginary) ProcessForward_Mono(float* input) {
            Array.Copy(inputStreamForward, 0, inputStreamForward, subbands, coeffs.Length - subbands);
            fixed (float* pInputStream = inputStreamForward, pCoeffs = coeffs, pGrouping = grouping) {
                float* inputPos = input + subbands - 1,
                    inputStreamPos = pInputStream,
                    end = inputStreamPos + subbands;
                while (inputStreamPos != end) {
                    *inputStreamPos++ = *inputPos--;
                }

                QMath.MultiplyAndSet_Mono(pInputStream, pCoeffs, pGrouping, doubleLength);
                for (int sample = doubleLength; sample < window.Length; sample += doubleLength) {
                    QMath.MultiplyAndAdd_Mono(pInputStream + sample, pCoeffs + sample, pGrouping, doubleLength);
                }

                for (int sb = 0; sb < subbands; sb++) {
                    fixed (float* real = forwardCache[sb].real, imaginary = forwardCache[sb].imaginary) {
                        outCache.real[sb] = QMath.MultiplyAndAdd_Mono(real, pGrouping, doubleLength);
                        outCache.imaginary[sb] = QMath.MultiplyAndAdd_Mono(imaginary, pGrouping, doubleLength);
                    }
                }
            }
            return outCache;
        }

        /// <summary>
        /// Convert a timeslot of QMFB <see cref="subbands"/> to PCM samples.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public unsafe void ProcessInverse_Mono((float[] real, float[] imaginary) input, float* output) {
            Array.Copy(inputStreamInverse, 0, inputStreamInverse, doubleLength, inputStreamInverse.Length - doubleLength);

            fixed (float* pInputStreamInverse = inputStreamInverse, pCoeffs = coeffs) {
                fixed (float* cacheReal = inverseCache[0].real, cacheImaginary = inverseCache[0].imaginary) {
                    QMath.MultiplyAndSet_Mono(cacheReal, input.real[0], cacheImaginary, -input.imaginary[0],
                        pInputStreamInverse, doubleLength);
                }
                for (int sb = 1; sb < subbands; ++sb) {
                    fixed (float* cacheReal = inverseCache[sb].real, cacheImaginary = inverseCache[sb].imaginary) {
                        QMath.MultiplyAndAdd_Mono(cacheReal, input.real[sb], cacheImaginary, -input.imaginary[sb],
                            pInputStreamInverse, doubleLength);
                    }
                }

                QMath.MultiplyAndSet_Mono(pInputStreamInverse, pCoeffs,
                        pInputStreamInverse + subbands * 3, pCoeffs + subbands, output, subbands);
                for (int j = 1, end = coeffs.Length / doubleLength; j < end; j++) {
                    int timeSlot = subbands * 4 * j;
                    int coeffSlot = doubleLength * j;
                    int timePair = timeSlot + subbands * 3;
                    int coeffPair = coeffSlot + subbands;
                    QMath.MultiplyAndAdd_Mono(pInputStreamInverse + timeSlot, pCoeffs + coeffSlot,
                        pInputStreamInverse + timePair, pCoeffs + coeffPair, output, subbands);
                }
            }
        }
    }
}