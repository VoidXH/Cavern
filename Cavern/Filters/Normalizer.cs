using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Makes sure the content always stays as close to 0 dB as possible.
    /// </summary>
    public class Normalizer : Filter {
        /// <summary>
        /// Gain increment per frame, should be decay rate * update rate / sample rate.
        /// </summary>
        public float decayFactor;

        /// <summary>
        /// Don't go over 0 dB gain. If true, the normalizer will act as a clipping protector.
        /// </summary>
        public bool limiterOnly;

        /// <summary>
        /// Last normalizer gain.
        /// </summary>
        float lastGain = 1;

        /// <summary>
        /// Create a normalizer or limiter.
        /// </summary>
        public Normalizer(bool limiterOnly) => this.limiterOnly = limiterOnly;

        /// <summary>
        /// Apply normalization on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            float max = Math.Abs(samples[0]), absSample;
            for (int sample = 1; sample < samples.Length; sample++) {
                absSample = Math.Abs(samples[sample]);
                if (max < absSample) {
                    max = absSample;
                }
            }
            if (max * lastGain > 1) { // Attack
                lastGain = .9f / max;
            }
            WaveformUtils.Gain(samples, lastGain); // Normalize last samples
            // Decay
            lastGain += decayFactor;
            if (limiterOnly && lastGain > 1) {
                lastGain = 1;
            }
        }
    }
}