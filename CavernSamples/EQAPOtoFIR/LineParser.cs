using System.Globalization;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Equalization;

namespace EQAPOtoFIR {
    /// <summary>
    /// Functions to parse a single line of an Equalizer APO configuration file.
    /// </summary>
    public static class LineParser {
        /// <summary>
        /// Parse a line of Equalizer APO configuration and apply the changes on a channel.
        /// </summary>
        public static void Parse(string line, EqualizedChannel target) {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) {
                return;
            }
            string[] split = line.Split(':');
            switch (split[0]) {
                case "Preamp":
                    target.Modify(Preamp(split[1]));
                    break;
                case "Delay":
                    Delay(split[1], target);
                    break;
                case "Filter":
                    target.Modify(Filter(split[1]));
                    break;
                case "GraphicEQ":
                    target.Modify(GraphicEQ(split[1]));
                    break;
            }
        }

        /// <summary>
        /// Parse gains from any number format and culture.
        /// </summary>
        static bool ParseGain(string from, out double gain) =>
            double.TryParse(from.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out gain);

        /// <summary>
        /// Parse a preamplification as an EQ.
        /// </summary>
        static Equalizer Preamp(string source) {
            source = source.TrimStart();
            if (!ParseGain(source.Substring(0, source.IndexOf(' ')), out double gain)) {
                return null;
            }
            Equalizer eq = new Equalizer();
            eq.AddBand(new Band(1000, gain));
            return eq;
        }

        /// <summary>
        /// Parse a delay and set the corresponding delay value in <paramref name="channel"/>.
        /// </summary>
        static void Delay(string source, EqualizedChannel channel) {
            string[] split = source.Trim().Split(' ');
            if (ParseGain(split[0], out double delay)) {
                if (split[1].Equals("ms")) {
                    channel.AddDelay(delay);
                } else {
                    channel.AddDelay((int)delay);
                }
            }
        }

        /// <summary>
        /// Parse a peaking filter line.
        /// Sample with Q factor: ON PK Fc 100 Hz Gain 0 dB Q 10
        /// Sample with bandwidth: ON PK Fc 100 Hz Gain 0 dB BW Oct 0.1442
        /// </summary>
        static PeakingEQ ParsePeakingEQ(string[] split) {
            if (ParseGain(split[3], out double freq) && ParseGain(split[6], out double gain)) {
                if (split[8].Equals("Q") && ParseGain(split[9], out double q)) {
                    return new PeakingEQ(analyzerSampleRate, freq, q, gain);
                } else if (split[8].Equals("BW") && ParseGain(split[10], out double bw)) {
                    return new PeakingEQ(analyzerSampleRate, freq, QFactor.FromBandwidth(bw), gain);
                }
            }
            return null;
        }

        /// <summary>
        /// Parse a low-pass filter line.
        /// Sample: ON LP Fc 100 Hz
        /// </summary>
        static Lowpass ParseLowpass(string[] split) =>
            ParseGain(split[3], out double freq) ? new Lowpass(analyzerSampleRate, freq) : null;

        /// <summary>
        /// Parse a low-pass filter line with Q factor.
        /// Sample: ON LPQ Fc 100 Hz Q 0.7071
        /// </summary>
        static Lowpass ParseLowpassWithQ(string[] split) =>
            ParseGain(split[3], out double freq) && ParseGain(split[6], out double q) ? new Lowpass(analyzerSampleRate, freq, q) : null;

        /// <summary>
        /// Parse a high-pass filter line.
        /// Sample: ON HP Fc 100 Hz
        /// </summary>
        static Highpass ParseHighpass(string[] split) =>
            ParseGain(split[3], out double freq) ? new Highpass(analyzerSampleRate, freq) : null;

        /// <summary>
        /// Parse a high-pass filter line with Q factor.
        /// Sample: ON HPQ Fc 100 Hz Q 0.7071
        /// </summary>
        static Highpass ParseHighpassWithQ(string[] split) =>
            ParseGain(split[3], out double freq) && ParseGain(split[6], out double q) ? new Highpass(analyzerSampleRate, freq, q) : null;

        /// <summary>
        /// Parse a band-pass filter line.
        /// Sample: ON BP Fc 100 Hz
        /// Sample with Q-factor: ON BP Fc 100 Hz Q 10
        /// </summary>
        static Bandpass ParseBandpass(string[] split) {
            if (ParseGain(split[3], out double freq)) {
                if (split.Length < 6) {
                    return new Bandpass(analyzerSampleRate, freq);
                } else if (ParseGain(split[6], out double q)) {
                    return new Bandpass(analyzerSampleRate, freq, q);
                }
            }
            return null;
        }

        /// <summary>
        /// Parse a low-shelf filter line.
        /// Sample: ON LS Fc 100 Hz Gain 0 dB
        /// </summary>
        static LowShelf ParseLowShelf(string[] split) =>
            ParseGain(split[3], out double freq) && ParseGain(split[6], out double gain)
                ? new LowShelf(analyzerSampleRate, freq, QFactor.FromSlope(0.9, gain), gain)
                : null;

        /// <summary>
        /// Parse a low-shelf filter line with slope.
        /// Sample: ON LSC 12 dB Fc 100 Hz Gain 0 dB
        /// </summary>
        static LowShelf ParseLowShelfWithSlope(string[] split) =>
            ParseGain(split[2], out double slope) && ParseGain(split[5], out double freq) && ParseGain(split[8], out double gain)
                ? new LowShelf(analyzerSampleRate, freq, QFactor.FromSlopeDecibels(slope, gain), gain)
                : null;

        /// <summary>
        /// Parse a high-shelf filter line.
        /// Sample: ON HS Fc 100 Hz Gain 0 dB
        /// </summary>
        static HighShelf ParseHighShelf(string[] split) =>
            ParseGain(split[3], out double freq) && ParseGain(split[6], out double gain)
                ? new HighShelf(analyzerSampleRate, freq, QFactor.FromSlope(0.9, gain), gain)
                : null;

        /// <summary>
        /// Parse a high-shelf filter line with slope.
        /// Sample: ON HSC 12 dB Fc 100 Hz Gain 0 dB
        /// </summary>
        static HighShelf ParseHighShelfWithSlope(string[] split) =>
            ParseGain(split[2], out double slope) && ParseGain(split[5], out double freq) && ParseGain(split[8], out double gain)
                ? new HighShelf(analyzerSampleRate, freq, QFactor.FromSlopeDecibels(slope, gain), gain)
                : null;

        /// <summary>
        /// Parse a notch filter line.
        /// Sample: ON NO Fc 100 Hz
        /// Sample with Q-factor: ON NO Fc 100 Hz Q 30
        /// </summary>
        static Notch ParseNotch(string[] split) {
            if (ParseGain(split[3], out double freq)) {
                if (split.Length < 6) {
                    return new Notch(analyzerSampleRate, freq, 30);
                } else if (ParseGain(split[6], out double q)) {
                    return new Notch(analyzerSampleRate, freq, q);
                }
            }
            return null;
        }

        /// <summary>
        /// Parse an all-pass filter line.
        /// Sample: ON AP Fc 100 Hz Q 10
        /// </summary>
        static Allpass ParseAllpass(string[] split) {
            if (ParseGain(split[3], out double freq) && ParseGain(split[6], out double q)) {
                return new Allpass(analyzerSampleRate, freq, q);
            }
            return null;
        }

        /// <summary>
        /// Parse a biquad filter and generate an Equalizer that simulates it.
        /// </summary>
        static BiquadFilter Filter(string source) {
            string[] split = source.Trim().Split(' ');
            BiquadFilter filter = split[1] switch {
                "PK" => ParsePeakingEQ(split),
                "LP" => ParseLowpass(split),
                "LPQ" => ParseLowpassWithQ(split),
                "HP" => ParseHighpass(split),
                "HPQ" => ParseHighpassWithQ(split),
                "BP" => ParseBandpass(split),
                "LS" => ParseLowShelf(split),
                "LSC" => ParseLowShelfWithSlope(split),
                "HS" => ParseHighShelf(split),
                "HSC" => ParseHighShelfWithSlope(split),
                "NO" => ParseNotch(split),
                "AP" => ParseAllpass(split),
                _ => null
            };
            if (filter == null) {
                return null;
            }
            return filter;
        }

        /// <summary>
        /// Parse a graphic EQ.
        /// </summary>
        static Equalizer GraphicEQ(string source) {
            Equalizer eq = new Equalizer();
            string[] split = source.Split(';');
            for (int i = 0; i < split.Length; ++i) {
                string[] band = split[i].Trim().Split(' ');
                if (ParseGain(band[0].Replace(',', '.'), out double freq) && ParseGain(band[1].Replace(',', '.'), out double gain)) {
                    eq.AddBand(new Band(freq, gain));
                }
            }
            return eq;
        }


        /// <summary>
        /// Sample rate used for biquad filter simulation.
        /// </summary>
        const int analyzerSampleRate = 48000;
    }
}