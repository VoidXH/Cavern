using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order biquad filter.
    /// </summary>
    public abstract partial class BiquadFilter : Filter, IEqualizerAPOFilter, ILocalizableToString, IResettableFilter, ISampleRateDependentFilter, IXmlSerializable {
        /// <inheritdoc/>
        [IgnoreDataMember]
        public int SampleRate {
            get => sampleRate;
            set {
                sampleRate = value;
                Reset(centerFreq, q, gain);
            }
        }
        int sampleRate;

        /// <summary>
        /// Center frequency (-3 dB point) of the filter.
        /// </summary>
        [DisplayName("Center frequency (Hz)")]
        public double CenterFreq {
            get => centerFreq;
            set => Reset(value, q, gain);
        }

        /// <summary>
        /// Q-factor of the filter.
        /// </summary>
        [DisplayName("Q-factor")]
        public double Q {
            get => q;
            set => Reset(centerFreq, value, gain);
        }

        /// <summary>
        /// Gain of the filter in decibels.
        /// </summary>
        [DisplayName("Gain (dB)")]
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
        /// The enumerated type of this filter.
        /// </summary>
        public abstract BiquadFilterType FilterType { get; }

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
            SampleRate = sampleRate;
            Reset(centerFreq, q, gain);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Reset() {
            x1 = 0;
            x2 = 0;
            y1 = 0;
            y2 = 0;
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
            float w0 = (float)(MathF.PI * 2 * centerFreq / SampleRate), cos = (float)Math.Cos(w0),
                alpha = (float)(Math.Sin(w0) / (q + q)), divisor = 1 / (1 + alpha);
            Reset(cos, alpha, divisor);
        }

        /// <summary>
        /// Create a copy of this filter with a changed <see cref="SampleRate"/>.
        /// </summary>
        /// <param name="sampleRate">Sample rate of the new filter</param>
        public abstract object Clone(int sampleRate);

        /// <inheritdoc/>
        public abstract void ExportToEqualizerAPO(List<string> wipConfig);

        /// <summary>
        /// Clone the filter with an inverse gain.
        /// </summary>
        public BiquadFilter GetInverse() => Create(FilterType, sampleRate, centerFreq, q, -gain);

        /// <summary>
        /// Calculate the maximum distance of the filter's poles from the origin.
        /// </summary>
        public float GetPoleRadius() {
            float discriminant = a1 * a1 - 4 * a2;
            return discriminant >= 0
                ? MathF.Max(Math.Abs((-a1 + MathF.Sqrt(discriminant)) * .5f), Math.Abs((-a1 - MathF.Sqrt(discriminant)) * .5f))
                : MathF.Sqrt(a2);
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
            b0 = MathF.Pow(10, (float)gain * .025f) * b2;
        }
    }
}
