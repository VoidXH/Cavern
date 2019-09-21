﻿using System;

namespace Cavern.Filters {
    /// <summary>Normalized feedforward comb filter.</summary>
    /// <remarks>The feedback comb filter is called <see cref="Echo"/>.</remarks>
    public class Comb : Filter {
        /// <summary>Delay in samples.</summary>
        public int K {
            get => delay.DelaySamples;
            set => delay.DelaySamples = value;
        }

        /// <summary>First minimum point.</summary>
        public float Frequency {
            get => sampleRate * .5f / K;
            set => K = (int)(.5f / (value / sampleRate) + 1);
        }

        /// <summary>Wet mix multiplier.</summary>
        public float Alpha;

        /// <summary>Delay filter generating the samples fed forward.</summary>
        readonly Delay delay;
        /// <summary>Array used to hold samples processed by <see cref="delay"/>.</summary>
        float[] cache = new float[0];
        /// <summary>Cached source sample rate.</summary>
        readonly int sampleRate;

        /// <summary>Normalized feedforward comb filter.</summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="K">Delay in samples</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, int K, float alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay(K);
        }

        /// <summary>Normalized feedforward comb filter.</summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="frequency">First minimum point</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, float frequency, float alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay((int)(.5f / (frequency / sampleRate) + 1));
        }

        /// <summary>Apply comb on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] samples) {
            if (cache.Length != samples.Length)
                cache = new float[samples.Length];
            Array.Copy(samples, cache, samples.Length);
            delay.Process(cache);
            float divisor = 1 / (1 + Alpha);
            for (int sample = 0; sample < samples.Length; ++sample)
                samples[sample] = (samples[sample] + cache[sample] * Alpha) * divisor;
        }
    }
}