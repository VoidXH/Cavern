using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order biquad filter.
    /// </summary>
    public abstract class BiquadFilter : Filter, IEqualizerAPOFilter, ILocalizableToString {
        /// <summary>
        /// Sample rate of the filter.
        /// </summary>
        public int SampleRate { get; protected set; }

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

        /// <summary>
        /// Construct a <see cref="BiquadFilter"/> with the desired parameters.
        /// </summary>
        /// <param name="type">Selected kind of biquad filter</param>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public static BiquadFilter Create(BiquadFilterType type, int sampleRate, double centerFreq, double q, double gain) => type switch {
            BiquadFilterType.Allpass => new Allpass(sampleRate, centerFreq, q, gain),
            BiquadFilterType.Bandpass => new Bandpass(sampleRate, centerFreq, q, gain),
            BiquadFilterType.Highpass => new Highpass(sampleRate, centerFreq, q, gain),
            BiquadFilterType.HighShelf => new HighShelf(sampleRate, centerFreq, q, gain),
            BiquadFilterType.Lowpass => new Lowpass(sampleRate, centerFreq, q, gain),
            BiquadFilterType.LowShelf => new LowShelf(sampleRate, centerFreq, q, gain),
            BiquadFilterType.Notch => new Notch(sampleRate, centerFreq, q, gain),
            BiquadFilterType.PeakingEQ => new PeakingEQ(sampleRate, centerFreq, q, gain),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="BiquadFilter"/> filter,
        /// detecting its type.<br />
        /// </summary>
        public static BiquadFilter FromEqualizerAPO(string[] splitLine, int sampleRate) => splitLine[2].ToUpperInvariant() switch {
            "PK" => PeakingEQ.FromEqualizerAPO(splitLine, sampleRate),
            "LP" => Lowpass.FromEqualizerAPO(splitLine, sampleRate),
            "LPQ" => Lowpass.FromEqualizerAPO(splitLine, sampleRate),
            "HP" => Highpass.FromEqualizerAPO(splitLine, sampleRate),
            "HPQ" => Highpass.FromEqualizerAPO(splitLine, sampleRate),
            "BP" => Bandpass.FromEqualizerAPO(splitLine, sampleRate),
            "LS" => LowShelf.FromEqualizerAPO(splitLine, sampleRate),
            "LSC" => LowShelf.FromEqualizerAPO(splitLine, sampleRate),
            "HS" => HighShelf.FromEqualizerAPO(splitLine, sampleRate),
            "HSC" => HighShelf.FromEqualizerAPO(splitLine, sampleRate),
            "NO" => Notch.FromEqualizerAPO(splitLine, sampleRate),
            "AP" => Allpass.FromEqualizerAPO(splitLine, sampleRate),
            _ => throw new ArgumentOutOfRangeException(splitLine[2])
        };

        /// <summary>
        /// Wipe the history to prevent clipping when applying the same filter for a new signal.
        /// </summary>
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

        /// <summary>
        /// Create a copy of this filter with a changed <see cref="SampleRate"/>.
        /// </summary>
        /// <param name="sampleRate">Sample rate of the new filter</param>
        public abstract object Clone(int sampleRate);

        /// <inheritdoc/>
        public abstract void ExportToEqualizerAPO(List<string> wipConfig);

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

        /// <summary>
        /// Display the filter's parameters when converting to string.
        /// </summary>
        public override string ToString() => $"{FilterType} at {centerFreq} Hz, Q: {q}, gain: {gain} dB";

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" => $"{FilterType} {centerFreq} Hz-en, Q: {q}, erősítés: {gain} dB",
            _ => ToString()
        };
    }
}