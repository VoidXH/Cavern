using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace QuickEQResultMerger {
    /// <summary>
    /// A file that contains gain and delay values.
    /// </summary>
    class ResultFile {
        /// <summary>
        /// The gain/delay pairs contained in the file.
        /// </summary>
        public readonly List<Measurement> measurements = new();

        /// <summary>
        /// The separator used by the system for parsing floats.
        /// </summary>
        readonly char separator;

        /// <summary>
        /// The separator that's not used by the system but could be in the file.
        /// </summary>
        readonly char wrongSeparator;

        /// <summary>
        /// Last parsed channel label.
        /// </summary>
        string lastChannel;

        /// <summary>
        /// Last parsed gain value.
        /// </summary>
        string lastGain;

        /// <summary>
        /// Last parsed delay value.
        /// </summary>
        string lastDelay;

        /// <summary>
        /// Parse a numeric value from a file, without its unit.
        /// </summary>
        static string GetNumericValue(string humanReadable) {
            int index = humanReadable.IndexOf(' ');
            return index > 0 ? humanReadable[..index] : humanReadable;
        }

        /// <summary>
        /// Parse a file that contains gain and delay values.
        /// </summary>
        public ResultFile(string path) {
            separator = 0.1f.ToString(CultureInfo.CurrentCulture)[1];
            wrongSeparator = separator == ',' ? '.' : ',';

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines) {
                int cut = line.IndexOf(':');
                if (cut < 0) {
                    continue;
                }

                string value = line[(cut + 2)..];
                switch (line[..cut]) {
                    case "Channel":
                        ParseLastChannel();
                        lastChannel = value;
                        break;
                    case "Gain":
                    case "Level":
                    case "Preamp":
                        lastGain = GetNumericValue(value);
                        break;
                    case "Delay":
                        lastDelay = GetNumericValue(value);
                        break;
                }
            }
            ParseLastChannel();
        }

        /// <summary>
        /// Try to merge all the read values into a channel correction entry.
        /// </summary>
        void ParseLastChannel() {
            if (lastChannel != null) {
                float gain = 0,
                    delay = 0;
                if (lastGain != null) {
                    _ = float.TryParse(lastGain.Replace(wrongSeparator, separator), out gain);
                }
                if (lastDelay != null) {
                    _ = float.TryParse(lastDelay.Replace(wrongSeparator, separator), out delay);
                }
                measurements.Add(new(lastChannel, gain, delay));
                lastChannel = null;
                lastGain = null;
                lastDelay = null;
            }
        }
    }
}