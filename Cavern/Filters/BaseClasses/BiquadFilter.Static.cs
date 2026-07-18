using System;

namespace Cavern.Filters {
    partial class BiquadFilter {
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
    }
}
