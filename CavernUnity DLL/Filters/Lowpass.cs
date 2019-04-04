using UnityEngine;

namespace Cavern.Filters {
    /// <summary>Simple first-order lowpass filter.</summary>
    internal class Lowpass : Filter {
        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        public float CenterFreq {
            get => _CenterFreq;
            set => Reset(value, Q);
        }
        /// <summary>Q-factor of the filter.</summary>
        public float Q {
            get => _Q;
            set => Reset(_CenterFreq, value);
        }

        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        float _CenterFreq;
        /// <summary>Q-factor of the filter.</summary>
        float _Q;
        /// <summary>Cached sample rate.</summary>
        int SampleRate;
        float a1, a2, b1, b2; // Transfer function
        float x1, x2, y1, y2; // History

        public Lowpass(float CenterFreq, float Q = .7071067811865475f) => Reset(CenterFreq, Q);

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public void Reset(float CenterFreq, float Q = .7071067811865475f) {
            SampleRate = AudioListener3D.Current != null ? AudioListener3D.Current.SampleRate : 48000;
            _CenterFreq = CenterFreq;
            _Q = Q;
            float w0 = Mathf.PI * 2 * CenterFreq / SampleRate, Cos = Mathf.Cos(w0), Alpha = Mathf.Sin(w0) / (Q + Q), Divisor = 1 / (1 + Alpha);
            a1 = -2 * Cos * Divisor;
            a2 = (1 - Alpha) * Divisor;
            b2 = (b1 = (1 - Cos) * Divisor) * .5f;
        }

        /// <summary>Apply lowpass on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] Samples) => Process(Samples, 0, 1);

        /// <summary>Apply lowpass on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public void Process(float[] Samples, int Channel, int Channels) {
            if (SampleRate != (AudioListener3D.Current != null ? AudioListener3D.Current.SampleRate : 48000))
                Reset(_CenterFreq, _Q);
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