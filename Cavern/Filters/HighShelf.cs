﻿using System;
using System.Collections.Generic;

using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order high shelf filter.
    /// </summary>
    public class HighShelf : BiquadFilter {
        /// <inheritdoc/>
        public override BiquadFilterType FilterType => BiquadFilterType.HighShelf;

        /// <summary>
        /// Simple first-order high shelf filter with maximum flatness and no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        public HighShelf(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <summary>
        /// Simple first-order high shelf filter with no additional gain.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        public HighShelf(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <summary>
        /// Simple first-order high shelf filter.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="centerFreq">Center frequency (-3 dB point) of the filter</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="gain">Gain of the filter in decibels</param>
        public HighShelf(int sampleRate, double centerFreq, double q, double gain) : base(sampleRate, centerFreq, q, gain) { }

        /// <summary>
        /// Parse a Filter line of Equalizer APO which was split at spaces to a Cavern <see cref="HighShelf"/> filter.<br />
        /// Sample with fixed shelf: Filter: ON HS Fc 100 Hz Gain 0 dB
        /// Sample with custom shelf: Filter: ON HSC 12 dB Fc 100 Hz Gain 0 dB
        /// </summary>
        public static new HighShelf FromEqualizerAPO(string[] splitLine, int sampleRate) {
            string type = splitLine[2].ToLowerInvariant();
            if (type == "hs" && QMath.TryParseDouble(splitLine[4], out double freq) && QMath.TryParseDouble(splitLine[5], out double gain)) {
                return new HighShelf(sampleRate, freq, QFactor.FromSlope(0.9, gain));
            } else if (type == "hsc" && QMath.TryParseDouble(splitLine[3], out double slope) &&
                QMath.TryParseDouble(splitLine[6], out freq) && QMath.TryParseDouble(splitLine[9], out gain)) {
                return new HighShelf(sampleRate, freq, QFactor.FromSlopeDecibels(slope, gain), gain);
            }
            throw new FormatException(nameof(splitLine));
        }

        /// <inheritdoc/>
        public override object Clone() => new HighShelf(SampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override object Clone(int sampleRate) => new HighShelf(sampleRate, centerFreq, q, gain);

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) {
            if (q == QFactor.reference) {
                wipConfig.Add($"Filter: ON HS Fc {centerFreq} Hz Gain {gain} dB");
            } else {
                wipConfig.Add($"Filter: ON HSC {QFactor.ToSlope(q, gain)} dB Fc {centerFreq} Hz Gain {gain} dB");
            }
        }

        /// <inheritdoc/>
        protected override void Reset(float cosW0, float alpha, float _) {
            float a = (float)Math.Pow(10, gain * .025f),
                slope = 2 * (float)Math.Sqrt(a) * alpha,
                minCos = (a - 1) * cosW0,
                addCos = (a + 1) * cosW0;
            float divisor = 1 / (a + 1 - minCos + slope);
            a1 = 2 * (a - 1 - addCos) * divisor;
            a2 = (a + 1 - minCos - slope) * divisor;
            b0 = a * (a + 1 + minCos + slope) * divisor;
            b1 = -2 * a * (a - 1 + addCos) * divisor;
            b2 = a * (a + 1 + minCos - slope) * divisor;
        }
    }
}