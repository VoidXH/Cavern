using Cavern.Filters;
using System.Numerics;

namespace Cavern.Remapping {
    /// <summary>
    /// Uses <see cref="SpectralDisassembler"/>s to create a better quality upmix than matrixing.
    /// </summary>
    public class DisassemblerUpmixer : PairBasedUpmixer {
        /// <summary>
        /// Smoothness of object movements, [0;1].
        /// </summary>
        public float smoothness = .8f;

        /// <summary>
        /// Intermediate source calculators.
        /// </summary>
        readonly SpectralDisassembler[] disassemblers;

        /// <summary>
        /// Content sample rate.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Uses <see cref="SpectralDisassembler"/>s to create a better quality upmix than matrixing.
        /// </summary>
        /// <param name="positions">Location of the input channels in the environment</param>
        /// <param name="pairs">Pairs of indices of later given inputs to recreate the space between</param>
        /// <param name="intermediateSourceCount">Number of bands to separate</param>
        /// <param name="sampleRate">Content sample rate</param>
        public DisassemblerUpmixer(Vector3[] positions, (int, int)[] pairs, int intermediateSourceCount, int sampleRate) :
            base(positions, pairs, intermediateSourceCount, sampleRate) {
            disassemblers = new SpectralDisassembler[pairs.Length];
            for (int i = 0; i < disassemblers.Length; i++) {
                disassemblers[i] = new SpectralDisassembler(intermediateSourceCount, sampleRate);
            }
            this.sampleRate = sampleRate;
        }

        /// <summary>
        /// Get the input samples, disassemble them, and place them in space.
        /// </summary>
        protected override float[][] UpdateSources(int samplesPerSource) {
            float smoothFactor = Cavernize.CalculateSmoothingFactor(sampleRate, samplesPerSource, smoothness);
            float[][] inputs = GetNewSamples(samplesPerSource);
            int source = 0;
            for (int i = 0; i < disassemblers.Length; i++) {
                disassemblers[i].smoothnessFactor = smoothFactor;
                int pairA = pairs[i].Item1,
                    pairB = pairs[i].Item2;
                SpectralDisassembler.SpectralPart[] intermediates = disassemblers[i].Process(inputs[pairA], inputs[pairB]);

                for (int j = 0; j < intermediates.Length; j++) {
                    IntermediateSources[source].Position = Vector3.Lerp(positions[pairA], positions[pairB], intermediates[j].panning);
                    output[source] = intermediates[j].samples;
                    ++source;
                }
            }
            return output;
        }
    }
}