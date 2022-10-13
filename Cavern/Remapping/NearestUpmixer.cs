using System;
using System.Numerics;

using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Upmixes channels with interpolated positions between sources.
    /// </summary>
    public class NearestUpmixer : PairBasedUpmixer {
        /// <summary>
        /// Upmixes channels with interpolated positions between sources.
        /// </summary>
        /// <param name="positions">Location of the input channels in the environment</param>
        /// <param name="pairs">Pairs of indices of later given inputs to recreate the space between</param>
        /// <param name="intermediateSourceCount">Number of bands to separate</param>
        /// <param name="sampleRate">Content sample rate</param>
        public NearestUpmixer(Vector3[] positions, (int, int)[] pairs, int intermediateSourceCount, int sampleRate) :
            base(positions, pairs, intermediateSourceCount, sampleRate) { }

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected override float[][] UpdateSources(int samplesPerSource) {
            float[][] inputs = GetNewSamples(samplesPerSource);
            int intermediateSourceCount = IntermediateSources.Length / pairs.Length;
            int source = 0;
            for (int pair = 0; pair < pairs.Length; pair++) {
                int pairA = pairs[pair].Item1,
                    pairB = pairs[pair].Item2;
                float[] inputA = inputs[pairA],
                        inputB = inputs[pairB];
                Vector3 positionA = positions[pairA],
                        positionB = positions[pairB];
                float p = 0,
                      pStep = 1f / (intermediateSourceCount - 1),
                      vTotalMul = pStep / vTotal;
                for (int i = 0; i < intermediateSourceCount; i++) {
                    IntermediateSources[source].Position = Vector3.Lerp(positionA, positionB, p);
                    WaveformUtils.Insert(inputA, output[source], MathF.Sqrt(p) * vTotalMul);
                    WaveformUtils.Mix(inputB, output[source++], MathF.Sqrt(1 - p) * vTotalMul);
                    p += pStep;
                }
            }
            return output;
        }

        /// <summary>
        /// To get the total voltage, you'd need to sum sqrt(p) and sqrt(1 - p) for all channels in <see cref="UpdateSources(int)"/>.
        /// This sum would take a long time to calculate, and setting gains after mixing would take even more.
        /// The result of sum(i: 1->n) sqrt(i/n) would be sqrt(1/n) * H(4, -1/2) where H is the generalized harmonic number.
        /// Graphed out, this result is more or less linear, we just need a good anchor for minimal error, and it's n = 10.
        /// The number you see here, is the result * 2 (for both the insert and the mix) / 10 (as it's the result at n = 10),
        /// and this way, the voltage total can be linearized.
        /// </summary>
        const float vTotal = 1.421018683413634796f;
    }
}