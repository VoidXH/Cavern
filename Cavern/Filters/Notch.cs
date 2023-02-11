using System;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order notch filter.
    /// </summary>
    public class Notch : BiquadFilter {
        /// <summary>
        /// Simple first-order notch filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Notch(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order notch filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Notch(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order notch filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Notch(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Regenerate the transfer function.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public override void Reset(double centerFreq, double q, double gain) {
            base.Reset(centerFreq, q, gain);
            float w0 = (float)(Math.PI * 2 * centerFreq / sampleRate), cos = (float)Math.Cos(w0),
                alpha = (float)(Math.Sin(w0) / (q + q)), divisor = 1 / (1 + alpha); // 1 / a0
            b0 = (float)Math.Pow(10, gain * .05f) * divisor;
            a1 = b1 = -2 * cos * (b2 = divisor);
            a2 = (1 - alpha) * divisor;
        }
    }
}