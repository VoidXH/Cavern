using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order peaking filter.
    /// </summary>
    public class PeakingEQ : BiquadFilter {
        /// <inheritdoc/>
        public override BiquadFilterType FilterType => BiquadFilterType.PeakingEQ;

        /// <summary>
        /// Simple first-order peaking filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public PeakingEQ(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order peaking filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public PeakingEQ(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order peaking filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public PeakingEQ(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="PeakingEQ"/> filter.<br />
        /// Sample with Q factor: Filter: ON PK Fc 100 Hz Gain 0 dB Q 10<br />
        /// Sample with bandwidth: Filter: ON PK Fc 100 Hz Gain 0 dB BW Oct 0.1442
        /// </summary>
        public static PeakingEQ FromEqualizerAPO(string[] splitLine, int sampleRate) {
            if (QMath.TryParseDouble(splitLine[4], out double freq) && QMath.TryParseDouble(splitLine[7], out double gain)) {
                if (splitLine[9].Equals("Q") && QMath.TryParseDouble(splitLine[10], out double q)) {
                    return new PeakingEQ(sampleRate, freq, q, gain);
                } else if (splitLine[9].Equals("BW") && QMath.TryParseDouble(splitLine[11], out double bw)) {
                    return new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(bw), gain);
                }
            }
            throw new FormatException(nameof(splitLine));
        }

        /// <inheritdoc/>
        public override object Clone() => new PeakingEQ(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new PeakingEQ(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) =>
            wipConfig.Add($"Filter: ON PK Fc {centerFreq} Hz Gain {gain} dB Q {q}");

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float _) {
            float a = (float)Math.Pow(10, gain * .025f); // A = sqrt(gain), hence /40 instead of /20
            float divisor = 1 / (1 + alpha / a);
            b0 = (1 + alpha * a) * divisor;
            b2 = (1 - alpha * a) * divisor;
            a1 = b1 = -2 * cosW0 * divisor;
            a2 = (1 - alpha / a) * divisor;
        }
    }
}