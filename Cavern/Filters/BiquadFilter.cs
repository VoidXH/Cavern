using Cavern.Filters.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order biquad filter.
    /// </summary>
    public abstract class BiquadFilter : Filter {
        /// <summary>
        /// Center frequency (-3 dB point) of the filter.
        /// </summary>
        public double CenterFreq {
            get => centerFreq;
            set => Reset(value, q, gain);
        }

        /// <summary>
        /// Q-factor of the filter.
        /// </summary>
        public double Q {
            get => q;
            set => Reset(centerFreq, value, gain);
        }

        /// <summary>
        /// Gain of the filter in decibels.
        /// </summary>
        public double Gain {
            get => gain;
            set => Reset(centerFreq, q, value);
        }

        /// <summary>
        /// Center frequency (-3 dB point) of the filter.
        /// </summary>
        protected double centerFreq;

        /// <summary>
        /// Q-factor of the filter.
        /// </summary>
        protected double q;

        /// <summary>
        /// Gain of the filter in decibels.
        /// </summary>
        protected double gain;

        /// <summary>
        /// Cached sample rate.
        /// </summary>
        protected int sampleRate;

        /// <summary>
        /// History sample.
        /// </summary>
        protected float x1, x2, y1, y2;

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Transfer function variable.
        /// </summary>
        public float a1 { get; protected set; }

        /// <summary>
        /// Transfer function variable.
        /// </summary>
        public float a2 { get; protected set; }

        /// <summary>
        /// Transfer function variable.
        /// </summary>
        public float b0 { get; protected set; } = 1;

        /// <summary>
        /// Transfer function variable.
        /// </summary>
        public float b1 { get; protected set; }

        /// <summary>
        /// Transfer function variable.
        /// </summary>
        public float b2 { get; protected set; }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Simple first-order biquad filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public BiquadFilter(int sampleRate, double centerFreq, double q = QFactor.reference, double gain = 0) {
            this.sampleRate = sampleRate;
            Reset(centerFreq, q, gain);
        }

        /// <summary>
        /// Regenerate the transfer function.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public virtual void Reset(double centerFreq, double q = QFactor.reference, double gain = 0) {
            this.centerFreq = centerFreq;
            this.q = q;
            this.gain = gain;
        }

        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            for (int sample = channel; sample < samples.Length; sample += channels) {
                float thisSample = samples[sample];
                samples[sample] = b2 * x2 + b1 * x1 + b0 * thisSample - a1 * y1 - a2 * y2;
                y2 = y1;
                y1 = samples[sample];
                x2 = x1;
                x1 = thisSample;
            }
        }
    }
}