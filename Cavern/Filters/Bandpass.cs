using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order bandpass filter.
    /// </summary>
    public class Bandpass : BiquadFilter {
        /// <inheritdoc/>
        public override BiquadFilterType FilterType => BiquadFilterType.Bandpass;

        /// <summary>
        /// Simple first-order bandpass filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Bandpass(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order bandpass filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Bandpass(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order bandpass filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Bandpass(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="Bandpass"/> filter.<br />
        /// Sample: Filter: ON BP Fc 100 Hz
        /// Sample with Q-factor: Filter: ON BP Fc 100 Hz Q 10
        /// </summary>
        public static Bandpass FromEqualizerAPO(string[] splitLine, int sampleRate) {
            if (QMath.TryParseDouble(splitLine[4], out double freq)) {
                if (splitLine.Length < 7) {
                    return new Bandpass(sampleRate, freq);
                } else if (QMath.TryParseDouble(splitLine[7], out double q)) {
                    return new Bandpass(sampleRate, freq, q);
                }
            }
            throw new FormatException(nameof(splitLine));
        }

        /// <inheritdoc/>
        public override object Clone() => new Bandpass(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new Bandpass(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) {
            if (q == QFactor.reference) {
                wipConfig.Add($"Filter: ON BP Fc {centerFreq} Hz");
            } else {
                wipConfig.Add($"Filter: ON BP Fc {centerFreq} Hz Q {q}");
            }
        }

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float divisor) {
            b1 = 0;
            b2 = -alpha * divisor;
            b0 = -b2 * (float)Math.Pow(10, gain * .05f);
            a1 = -2 * cosW0 * divisor;
            a2 = (1 - alpha) * divisor;
        }
    }
}