﻿using System;
using System.ComponentModel;

namespace Cavern.Filters {
    /// <summary>
    /// Normalized feedforward comb filter.
    /// </summary>
    /// <remarks>The feedback comb filter is called <see cref="Echo"/>.</remarks>
    public class Comb : Filter {
        /// <summary>
        /// Wet mix multiplier.
        /// </summary>
        [DisplayName("Alpha (ratio)")]
        public double Alpha { get; set; }

        /// <summary>
        /// Delay in samples.
        /// </summary>
        [DisplayName("K (samples)")]
        public int K {
            get => delay.DelaySamples;
            set => delay.DelaySamples = value;
        }

        /// <summary>
        /// First minimum point.
        /// </summary>
        [DisplayName("Frequency (Hz)")]
        public double Frequency {
            get => sampleRate * .5 / K;
            set => K = (int)(.5 / (value / sampleRate) + 1);
        }

        /// <summary>
        /// Delay filter generating the samples fed forward.
        /// </summary>
        readonly Delay delay;

        /// <summary>
        /// Array used to hold samples processed by <see cref="delay"/>.
        /// </summary>
        float[] cache = new float[0];

        /// <summary>
        /// Cached source sample rate.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Normalized feedforward comb filter.
        /// </summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="K">Delay in samples</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, int K, double alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay(K);
        }

        /// <summary>
        /// Normalized feedforward comb filter.
        /// </summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="frequency">First minimum point</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, double frequency, double alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay((int)(.5 / (frequency / sampleRate) + 1));
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            if (cache.Length != samples.Length) {
                cache = new float[samples.Length];
            }
            Array.Copy(samples, cache, samples.Length);
            delay.Process(cache);
            float alpha = (float)Alpha,
                divisor = 1 / (1 + alpha);
            for (int sample = 0; sample < samples.Length; sample++) {
                samples[sample] = (samples[sample] + cache[sample] * alpha) * divisor;
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new Comb(sampleRate, K, Alpha);
    }
}