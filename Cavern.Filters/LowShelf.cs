using System;

namespace Cavern.Filters {
    /// <summary>Simple first-order low shelf filter.</summary>
    public class LowShelf : BiquadFilter {
        /// <summary>Simple first-order low shelf filter.</summary>
        /// <param name="SampleRate">Audio sample rate</param>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        /// <param name="Gain">Gain of the filter in decibels</param>
        public LowShelf(int SampleRate, float CenterFreq, float Q = .7071067811865475f, float Gain = 0) : base(SampleRate, CenterFreq, Q, Gain) { }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        /// <param name="Gain">Gain of the filter in decibels</param>
        public override void Reset(float CenterFreq, float Q = .7071067811865475f, float Gain = 0) {
            base.Reset(CenterFreq, Q, Gain);
            float w0 = (float)(Math.PI * 2 * CenterFreq / SampleRate), Cos = (float)Math.Cos(w0), Alpha = (float)Math.Sin(w0) / (Q + Q),
                A = (float)Math.Pow(10, Gain * .05f), Slope = 2 * (float)Math.Sqrt(A) * Alpha, MinCos = (A - 1) * Cos, AddCos = (A + 1) * Cos,
                Divisor = 1 / (A + 1 + MinCos + Slope); // 1 / a0
            a1 = -2 * (A - 1 + AddCos) * Divisor;
            a2 = (A + 1 + MinCos - Slope) * Divisor;
            b0 = A * (A + 1 - MinCos + Slope) * Divisor;
            b1 = 2 * A * (A - 1 - AddCos) * Divisor;
            b2 = A * (A + 1 - MinCos - Slope) * Divisor;
        }
    }
}