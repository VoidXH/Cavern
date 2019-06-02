namespace Cavern.Filters {
    /// <summary>Simple first-order biquad filter.</summary>
    public abstract class BiquadFilter : Filter {
        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        public float CenterFreq {
            get => centerFreq;
            set => Reset(value, q, gain);
        }
        /// <summary>Q-factor of the filter.</summary>
        public float Q {
            get => q;
            set => Reset(centerFreq, value, gain);
        }

        /// <summary>Gain of the filter in decibels.</summary>
        public float Gain {
            get => gain;
            set => Reset(centerFreq, q, value);
        }

        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        protected float centerFreq;
        /// <summary>Q-factor of the filter.</summary>
        protected float q;
        /// <summary>Gain of the filter in decibels.</summary>
        protected float gain;
        /// <summary>Cached sample rate.</summary>
        protected int sampleRate;
        /// <summary>Transfer function variable.</summary>
        protected float a1, a2, b0 = 1, b1, b2;
        /// <summary>History sample.</summary>
        protected float x1, x2, y1, y2;

        /// <summary>Simple first-order biquad filter.</summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public BiquadFilter(int sampleRate, float centerFreq, float q = .7071067811865475f, float gain = 0) {
            this.sampleRate = sampleRate;
            Reset(centerFreq, q, gain);
        }

        /// <summary>Regenerate the transfer function.</summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public virtual void Reset(float centerFreq, float q = .7071067811865475f, float gain = 0) {
            this.centerFreq = centerFreq;
            this.q = q;
            this.gain = gain;
        }

        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            for (int Sample = channel, End = samples.Length; Sample < End; Sample += channels) {
                float ThisSample = samples[Sample];
                samples[Sample] = b2 * x2 + b1 * x1 + b0 * ThisSample - a1 * y1 - a2 * y2;
                y2 = y1;
                y1 = samples[Sample];
                x2 = x1;
                x1 = ThisSample;
            }
        }
    }
}