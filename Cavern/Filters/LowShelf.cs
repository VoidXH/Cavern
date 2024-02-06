using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order low shelf filter.
    /// </summary>
    public class LowShelf : BiquadFilter {
        /// <summary>
        /// Simple first-order low shelf filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public LowShelf(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order low shelf filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public LowShelf(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order low shelf filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public LowShelf(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <inheritdoc/>
        public override object Clone() => new LowShelf(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new LowShelf(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) {
            if (q == QFactor.reference) {
                wipConfig.Add($"Filter: ON LS Fc {centerFreq} Hz Gain {gain} dB");
            } else {
                wipConfig.Add($"Filter: ON LSC {QFactor.ToSlope(q, gain)} dB Fc {centerFreq} Hz Gain {gain} dB");
            }
        }

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float _) {
            float a = (float)Math.Pow(10, gain * .025f),
                slope = 2 * (float)Math.Sqrt(a) * alpha,
                minCos = (a - 1) * cosW0,
                addCos = (a + 1) * cosW0;
            float divisor = 1 / (a + 1 + minCos + slope);
            a1 = -2 * (a - 1 + addCos) * divisor;
            a2 = (a + 1 + minCos - slope) * divisor;
            b0 = a * (a + 1 - minCos + slope) * divisor;
            b1 = 2 * a * (a - 1 - addCos) * divisor;
            b2 = a * (a + 1 - minCos - slope) * divisor;
        }
    }
}