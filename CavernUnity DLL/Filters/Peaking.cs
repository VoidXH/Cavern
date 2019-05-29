using UnityEngine;

namespace Cavern.Filters {
    /// <summary>Simple first-order peaking filter.</summary>
    public class PeakingEQ : BiquadFilter {
        /// <summary>Simple first-order peaking filter.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        /// <param name="Gain">Gain of the filter in decibels</param>
        public PeakingEQ(float CenterFreq, float Q = .7071067811865475f, float Gain = 0) : base(CenterFreq, Q, Gain) { }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        /// <param name="Gain">Gain of the filter in decibels</param>
        public override void Reset(float CenterFreq, float Q = .7071067811865475f, float Gain = 0) {
            SampleRate = AudioListener3D.Current != null ? AudioListener3D.Current.SampleRate : 48000;
            base.Reset(CenterFreq, Q, Gain);
            float w0 = Mathf.PI * 2 * CenterFreq / SampleRate, Cos = Mathf.Cos(w0), Alpha = Mathf.Sin(w0) / (Q + Q), A = Mathf.Pow(10, Gain * .05f),
                Divisor = 1 / (1 + Alpha / A); // 1 / a0
            b0 = (1 + Alpha * A) * Divisor;
            b2 = (1 - Alpha * A) * Divisor;
            a1 = b1 = -2 * Cos * Divisor;
            a2 = (1 - Alpha / A) * Divisor;
        }
    }
}