using System;

using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class QuadratureMirrorFilterBank {
        /// <summary>
        /// Convert a timeslot of QMFB <see cref="subbands"/> to PCM samples.
        /// This version of the function is faster when <see cref="CavernAmp"/> is available under Mono.
        /// The performance under native .NET is better in the vectorized C# version.
        /// </summary>
        public unsafe void ProcessInverse_Amp(float* inReal, float* inImaginary, float[] output) {
            Array.Copy(inputStreamInverse, 0, inputStreamInverse, doubleLength, inputStreamInverse.Length - doubleLength);

            fixed (float* inverse = inputStreamInverse) {
                fixed (float* cacheReal = inverseCache[0].real, cacheImaginary = inverseCache[0].imaginary) {
                    CavernAmp.MultiplyAndSet(cacheReal, inReal[0], cacheImaginary, -inImaginary[0], inverse, doubleLength);
                }
                for (int sb = 1; sb < subbands; ++sb) {
                    fixed (float* cacheReal = inverseCache[sb].real, cacheImaginary = inverseCache[sb].imaginary) {
                        CavernAmp.MultiplyAndAdd(cacheReal, inReal[sb], cacheImaginary, -inImaginary[sb], inverse, doubleLength);
                    }
                }

                fixed (float* pCoeffs = coeffs, pOutput = output) {
                    CavernAmp.MultiplyAndSet(inverse, pCoeffs,
                            inverse + subbands * 3, pCoeffs + subbands, pOutput, subbands);
                    for (int j = 1, end = coeffs.Length / doubleLength; j < end; j++) {
                        int timeSlot = subbands * 4 * j;
                        int coeffSlot = doubleLength * j;
                        int timePair = timeSlot + subbands * 3;
                        int coeffPair = coeffSlot + subbands;
                        CavernAmp.MultiplyAndAdd(inverse + timeSlot, pCoeffs + coeffSlot,
                            inverse + timePair, pCoeffs + coeffPair, pOutput, subbands);
                    }
                }
            }
        }
    }
}