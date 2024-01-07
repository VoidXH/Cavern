using System;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order peaking filter.
    /// </summary>
    public class PeakingEQ : BiquadFilter {
        /// <summary>
        /// Simple first-order peaking filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public PeakingEQ(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order peaking filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public PeakingEQ(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order peaking filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public PeakingEQ(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Create a copy of this filter.
        /// </summary>
        public override object Clone() => new PeakingEQ(SampleRate, centerFreq, q, gain);

        /// <summary>
        /// Create a copy of this filter with a changed sampleRate.
        /// </summary>
        public override object Clone(int sampleRate) => new PeakingEQ(sampleRate, centerFreq, q, gain);

        /// <summary>
        /// Reset the parameters specifically for the derived filter.
        /// </summary>
        /// <param name="cosW0">Cosine of omega0</param>
        /// <param name="alpha">Value of the alpha parameter</param>
        /// <param name="_">Would be the divisor, but it's calculated differently for this filter</param>
        protected override void Reset(float cosW0, float alpha, float _) {
            float a = (float)Math.Pow(10, gain * .025f); // gain is doubled for some reason
            float divisor = 1 / (1 + alpha / a);
            b0 = (1 + alpha * a) * divisor;
            b2 = (1 - alpha * a) * divisor;
            a1 = b1 = -2 * cosW0 * divisor;
            a2 = (1 - alpha / a) * divisor;
        }
    }
}