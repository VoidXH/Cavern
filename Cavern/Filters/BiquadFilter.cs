using System;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Simple first-order biquad filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        protected BiquadFilter(int sampleRate, double centerFreq) : this(sampleRate, centerFreq, QFactor.reference, 0) { }

        /// <summary>
        /// Simple first-order biquad filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        protected BiquadFilter(int sampleRate, double centerFreq, double q) : this(sampleRate, centerFreq, q, 0) { }

        /// <summary>
        /// Simple first-order biquad filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        protected BiquadFilter(int sampleRate, double centerFreq, double q, double gain) {
            this.sampleRate = sampleRate;
            Reset(centerFreq, q, gain);
        }

        /// <summary>
        /// Regenerate the transfer function with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public virtual void Reset(double centerFreq) => Reset(centerFreq, QFactor.reference, 0);

        /// <summary>
        /// Regenerate the transfer function with a custom Q-factor, but no additional gain.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public virtual void Reset(double centerFreq, double q) => Reset(centerFreq, q, 0);

        /// <summary>
        /// Regenerate the transfer function.
        /// </summary>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public void Reset(double centerFreq, double q, double gain) {
            this.centerFreq = centerFreq;
            this.q = q;
            this.gain = gain;
            float w0 = (float)(MathF.PI * 2 * centerFreq / sampleRate), cos = (float)Math.Cos(w0),
                alpha = (float)(Math.Sin(w0) / (q + q)), divisor = 1 / (1 + alpha);
            Reset(cos, alpha, divisor);
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

        /// <summary>
        /// Reset the parameters specifically for the derived filter.
        /// </summary>
        /// <param name="cosW0">Cosine of omega0</param>
        /// <param name="alpha">Value of the alpha parameter</param>
        /// <param name="divisor">1 / a0, as a0 is the same for all biquad filters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void Reset(float cosW0, float alpha, float divisor);

        /// <summary>
        /// Sets up a lowpass/highpass filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetupPass(float cosW0, float alpha, float divisor, float b1Pre) {
            a1 = -2 * cosW0 * divisor;
            a2 = (1 - alpha) * divisor;
            b1 = b1Pre * divisor;
            b2 = Math.Abs(b1) * .5f;
            b0 = MathF.Pow(10, (float)gain * .05f) * b2;
        }
    }
}