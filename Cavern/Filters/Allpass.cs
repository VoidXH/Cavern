using System;

using Cavern.Filters.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order allpass filter.
    /// </summary>
    public class Allpass : BiquadFilter {
        /// <summary>
        /// Simple first-order allpass filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Allpass(int sampleRate, double centerFreq, double q = QFactor.reference, double gain = 0) :
            base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Regenerate the transfer function.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public override void Reset(double centerFreq, double q = QFactor.reference, double gain = 0) {
            base.Reset(centerFreq, q, gain);
            float w0 = (float)(Math.PI * 2 * centerFreq / sampleRate), cos = (float)Math.Cos(w0),
                alpha = (float)(Math.Sin(w0) / (q + q)), divisor = 1 / (1 + alpha); // 1 / a0
            a2 = (1 - alpha) * divisor;
            b0 = (float)Math.Pow(10, gain * .05f) * a2;
            a1 = b1 = -2 * cos * divisor;
            b2 = 1;
        }
    }
}