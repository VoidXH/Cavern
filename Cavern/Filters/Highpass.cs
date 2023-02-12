using System;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order highpass filter.
    /// </summary>
    public class Highpass : BiquadFilter {
        /// <summary>
        /// Simple first-order highpass filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Highpass(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order highpass filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Highpass(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order highpass filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Highpass(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Reset the parameters specifically for the derived filter.
        /// </summary>
        /// <param name="cosW0">Cosine of omega0</param>
        /// <param name="alpha">Value of the alpha parameter</param>
        /// <param name="divisor">1 / a0, as a0 is the same for all biquad filters</param>
        protected override void Reset(float cosW0, float alpha, float divisor) {
            a1 = -2 * cosW0 * divisor;
            a2 = (1 - alpha) * divisor;
            b2 = -(b1 = (-1 - cosW0) * divisor) * .5f;
            b0 = (float)Math.Pow(10, gain * .05f) * b2;
        }
    }
}