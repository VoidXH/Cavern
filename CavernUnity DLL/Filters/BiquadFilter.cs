namespace Cavern.Filters {
    /// <summary>Simple first-order lowpass filter.</summary>
    public abstract class BiquadFilter : Filter {
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
        protected float _CenterFreq;
        /// <summary>Q-factor of the filter.</summary>
        protected float _Q;
        /// <summary>Cached sample rate.</summary>
        protected int SampleRate;
        /// <summary>Transfer function variable.</summary>
        protected float a1, a2, b1, b2;
        /// <summary>History sample.</summary>
        protected float x1, x2, y1, y2;

        /// <summary>Simple first-order biquad filter.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public BiquadFilter(float CenterFreq, float Q = .7071067811865475f) => Reset(CenterFreq, Q);

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="CenterFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="Q">Q-factor of the filter</param>
        public abstract void Reset(float CenterFreq, float Q = .7071067811865475f);

        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] Samples) => Process(Samples, 0, 1);

        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public override void Process(float[] Samples, int Channel, int Channels) {
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