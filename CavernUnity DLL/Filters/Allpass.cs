using UnityEngine;

namespace Cavern.Filters {
    /// <summary>Simple first-order allpass filter.</summary>
    public class Allpass : BiquadFilter {
        /// <summary>Simple first-order allpass filter.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public Allpass(float CenterFreq, float Q = .7071067811865475f) : base(CenterFreq, Q) { }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public override void Reset(float CenterFreq, float Q = .7071067811865475f) {
            SampleRate = AudioListener3D.Current != null ? AudioListener3D.Current.SampleRate : 48000;
            _CenterFreq = CenterFreq;
            _Q = Q;
            float w0 = Mathf.PI * 2 * CenterFreq / SampleRate, Cos = Mathf.Cos(w0), Alpha = Mathf.Sin(w0) / (Q + Q), Divisor = 1 / (1 + Alpha); // 1 / a0
            a2 = b0 = (1 - Alpha) * Divisor;
            a1 = b1 = -2 * Cos * Divisor;
            b2 = 1;
        }
    }
}