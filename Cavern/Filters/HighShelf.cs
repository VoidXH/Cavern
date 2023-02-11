using System;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order high shelf filter.
    /// </summary>
    public class HighShelf : BiquadFilter {
        /// <summary>
        /// Simple first-order high shelf filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public HighShelf(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order high shelf filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public HighShelf(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order high shelf filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public HighShelf(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Regenerate the transfer function.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public override void Reset(double centerFreq, double q, double gain) {
            base.Reset(centerFreq, q, gain);
            float w0 = (float)(Math.PI * 2 * centerFreq / sampleRate), cos = (float)Math.Cos(w0),
                alpha = (float)(Math.Sin(w0) / (q + q)), a = (float)Math.Pow(10, gain * .025f),
                slope = 2 * (float)Math.Sqrt(a) * alpha, minCos = (a - 1) * cos, addCos = (a + 1) * cos,
                divisor = 1 / (a + 1 - minCos + slope); // 1 / a0
            a1 = 2 * (a - 1 - addCos) * divisor;
            a2 = (a + 1 - minCos - slope) * divisor;
            b0 = a * (a + 1 + minCos + slope) * divisor;
            b1 = -2 * a * (a - 1 + addCos) * divisor;
            b2 = a * (a + 1 + minCos - slope) * divisor;
        }
    }
}