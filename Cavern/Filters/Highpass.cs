using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order highpass filter.
    /// </summary>
    public class Highpass : BiquadFilter {
        /// <inheritdoc/>
        public override BiquadFilterType FilterType => BiquadFilterType.Highpass;

        /// <summary>
        /// Simple first-order highpass filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Highpass(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order highpass filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Highpass(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order highpass filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Highpass(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="Highpass"/> filter.<br />
        /// Sample with fixed Q factor: Filter: ON HP Fc 100 Hz
        /// Sample with custom Q factor: Filter: ON HPQ Fc 100 Hz Q 0.7071
        /// </summary>
        public static Highpass FromEqualizerAPO(string[] splitLine, int sampleRate) {
            string type = splitLine[2].ToLower();
            if (type == "hp" && QMath.TryParseDouble(splitLine[4], out double freq)) {
                return new Highpass(sampleRate, freq);
            } else if (type == "hpq" && QMath.TryParseDouble(splitLine[4], out freq) && QMath.TryParseDouble(splitLine[7], out double q)) {
                return new Highpass(sampleRate, freq, q);
            }
            throw new FormatException(nameof(splitLine));
        }

        /// <inheritdoc/>
        public override object Clone() => new Highpass(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new Highpass(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) {
            if (q == QFactor.reference) {
                wipConfig.Add($"Filter: ON HP Fc {centerFreq} Hz");
            } else {
                wipConfig.Add($"Filter: ON HPQ Fc {centerFreq} Hz Q {q}");
            }
        }

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float divisor) => SetupPass(cosW0, alpha, divisor, -1 - cosW0);
    }
}