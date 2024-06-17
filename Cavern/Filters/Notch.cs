using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order notch filter.
    /// </summary>
    public class Notch : BiquadFilter {
        /// <inheritdoc/>
        public override BiquadFilterType FilterType => BiquadFilterType.Notch;

        /// <summary>
        /// Simple first-order notch filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public Notch(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order notch filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public Notch(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order notch filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public Notch(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="Notch"/> filter.<br />
        /// Sample with default Q-factor: Filter: ON NO Fc 100 Hz
        /// Sample with custom Q-factor: Filter: ON NO Fc 100 Hz Q 30
        /// </summary>
        public static new Notch FromEqualizerAPO(string[] splitLine, int sampleRate) {
            if (QMath.TryParseDouble(splitLine[4], out double freq)) {
                if (splitLine.Length < 7) {
                    return new Notch(sampleRate, freq, 30);
                } else if (QMath.TryParseDouble(splitLine[7], out double q)) {
                    return new Notch(sampleRate, freq, q);
                }
            }
            throw new FormatException(nameof(splitLine));
        }

        /// <inheritdoc/>
        public override object Clone() => new Notch(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new Notch(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) => wipConfig.Add($"Filter: ON NO Fc {centerFreq} Hz Q {q}");

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float divisor) {
            b0 = (float)Math.Pow(10, gain * .025f) * divisor;
            a1 = b1 = -2 * cosW0 * (b2 = divisor);
            a2 = (1 - alpha) * divisor;
        }
    }
}