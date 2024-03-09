using System;
using System.Collections.Generic;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order allpass filter.
    /// </summary>
    public class Allpass : PhaseSwappableBiquadFilter {
        /// <summary>
        /// Simple first-order allpass filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Allpass(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order allpass filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Allpass(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order allpass filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Allpass(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <inheritdoc/>
        public override object Clone() => new Allpass(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new Allpass(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) => wipConfig.Add($"Filter: ON AP Fc {centerFreq} Hz Q {q}");

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float divisor) {
            if (phaseSwapped) {
                divisor = 1 / (1 - alpha);
                a2 = (1 + alpha) * divisor;
            } else {
                a2 = (1 - alpha) * divisor;
            }
            b0 = (float)Math.Pow(10, gain * .025f) * a2;
            b2 = 1; // For APF, b2 = a0, and coefficients are divided by a0
            a1 = b1 = -2 * cosW0 * divisor;
        }
    }
}