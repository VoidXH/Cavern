using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>Simple first-order lowpass filter.</summary>
    internal class Lowpass {
        /// <summary>Cached sample rate.</summary>
        int SampleRate;
        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        float CenterFreq;
        /// <summary>Q-factor of the filter.</summary>
        float Q;
        float a1, a2, b1, b2; // Transfer function
        float x1, x2, y1, y2; // History

        public Lowpass(float CenterFreq, float Q) {
            Reset(CenterFreq, Q);
        }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public void Reset(float CenterFreq, float Q) {
            SampleRate = AudioListener3D.Current.SampleRate;
            this.CenterFreq = CenterFreq;
            this.Q = Q;
            float w0 = Mathf.PI * 2 * CenterFreq / SampleRate, Cos = Mathf.Cos(w0), Alpha = Mathf.Sin(w0) / (Q + Q), Divisor = 1 / (1 + Alpha);
            a1 = -2 * Cos * Divisor;
            a2 = (1 - Alpha) * Divisor;
            b2 = (b1 = (1 - Cos) * Divisor) * .5f;
        }

        /// <summary>Apply this filter to an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public void Process(float[] Samples, int Channel = 0, int Channels = 1) {
            if (SampleRate != AudioListener3D.Current.SampleRate)
                Reset(CenterFreq, Q);
            for (int Sample = Channel, End = Samples.Length; Sample < End; Sample += Channels) {
                float ThisSample = Samples[Sample];
                Samples[Sample] = b2 * (ThisSample + x2) + b1 * x1 - a1 * y1 - a2 * y2;
                y2 = y1;
                y1 = Samples[Sample];
                x2 = x1;
                x1 = ThisSample;
            }
        }
    }
}