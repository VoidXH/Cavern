using System.Globalization;

using Cavern.Filters;
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
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) {
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
                    target.Modify(BiquadFilter.FromEqualizerAPO(line.Split(' '), analyzerSampleRate));
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