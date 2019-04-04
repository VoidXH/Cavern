using UnityEngine;

namespace Cavern.Filters {
    /// <summary>Simple first-order lowpass filter.</summary>
    public class Lowpass : BiquadFilter {
        /// <summary>Simple first-order lowpass filter.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public Lowpass(float CenterFreq, float Q = .7071067811865475f) : base(CenterFreq, Q) { }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public override void Reset(float CenterFreq, float Q = .7071067811865475f) {
            SampleRate = AudioListener3D.Current != null ? AudioListener3D.Current.SampleRate : 48000;
            _CenterFreq = CenterFreq;
            _Q = Q;
            float w0 = Mathf.PI * 2 * CenterFreq / SampleRate, Cos = Mathf.Cos(w0), Alpha = Mathf.Sin(w0) / (Q + Q), Divisor = 1 / (1 + Alpha);
            a1 = -2 * Cos * Divisor;
            a2 = (1 - Alpha) * Divisor;
            b2 = (b1 = (1 - Cos) * Divisor) * .5f;
        }
    }
}