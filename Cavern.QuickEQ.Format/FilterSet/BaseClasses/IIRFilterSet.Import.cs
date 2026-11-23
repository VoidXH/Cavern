using System;
using System.Collections.Generic;

using Cavern.Filters;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    // Import handlers of generic PEQ files
    partial class IIRFilterSet {
        /// <summary>
        /// Parse a single-file export created with <see cref="Export()"/> at the given <paramref name="sampleRate"/>.
        /// The channel count is derived from the number of channel headers in the exported text.
        /// </summary>
        public IIRFilterSet(string contents, int sampleRate) : this(contents, sampleRate, Cavern.Filters.Utilities.QFactor.reference) { }

        /// <summary>
        /// Parse a single-file export created with <see cref="Export()"/> at the given <paramref name="sampleRate"/>.
        /// The channel count is derived from the number of channel headers in the exported text.
        /// Uses the specified <paramref name="defaultQ"/> for gain-only format lines that lack Q factors.
        /// </summary>
        /// <param name="contents">Text created by <see cref="Export()"/>.</param>
        /// <param name="sampleRate">Sample rate used to construct the parsed <see cref="PeakingEQ"/> filters.</param>
        /// <param name="defaultQ">Default Q factor to use when the export lacks Q factors (gain-only format).</param>
        public IIRFilterSet(string contents, int sampleRate, double defaultQ) : base(sampleRate) {
            List<(string Name, List<PeakingEQ> Filters)> parsed = ParseExport(contents, sampleRate, defaultQ);
            Initialize<IIRChannelData>(parsed.Count);
            for (int channel = 0; channel < parsed.Count; channel++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[channel];
                channelRef.name = parsed[channel].Name;
                channelRef.filters = parsed[channel].Filters.ToArray();
            }
        }

        /// <summary>
        /// Parse the output of <see cref="Export()"/> into per-channel filter sets, preserving channel order and names.
        /// Channels are counted from the section headers, so a channel with no filters is still represented.
        /// Handles both the full format (with Q factors) and the gain-only format (without Q factors).
        /// </summary>
        /// <param name="contents">Text created by <see cref="Export()"/>.</param>
        /// <param name="sampleRate">Sample rate used to construct the parsed <see cref="PeakingEQ"/> filters.</param>
        /// <param name="defaultQ">Default Q factor to use when the export lacks Q factors (gain-only format).</param>
        /// <returns>Channel name to its parsed peaking EQ filter set, in section order</returns>
        static List<(string Name, List<PeakingEQ> Filters)> ParseExport(string contents, int sampleRate, double defaultQ) {
            List<(string Name, List<PeakingEQ> Filters)> result = new List<(string, List<PeakingEQ>)>();
            string lastLine = string.Empty;
            int current = -1;
            double lastGain = double.NaN;
            double lastFrequency = double.NaN;
            double lastQ = double.NaN;
            bool hasQ = false;

            foreach (string line in contents.ReadLines()) {
                if (line.IsTheSameCharacter() == '=') {
                    result.Add((lastLine, new List<PeakingEQ>()));
                    current = result.Count - 1;
                } else if (line.StartsWith(GainLineStart)) {
                    lastGain = QMath.ParseDouble(line.ReadUntil(GainLineStart.Length, ' '));
                } else if (line.StartsWith(FrequencyLineStart)) {
                    lastFrequency = QMath.ParseDouble(line.ReadUntil(FrequencyLineStart.Length, ' '));
                } else if (line.StartsWith(QLineStart)) {
                    lastQ = QMath.ParseDouble(line.ReadUntil(QLineStart.Length, ' '));
                    hasQ = true;
                } else {
                    // Handle gain-only format: "freq Hz:\t gain dB"
                    (double freq, double gain) = TryParseGainOnlyLine(line);
                    if (freq > 0 && !double.IsNaN(gain)) {
                        lastFrequency = freq;
                        lastGain = gain;
                        lastQ = defaultQ;
                        hasQ = true;
                    }
                }

                if (current >= 0 && !double.IsNaN(lastFrequency) && !double.IsNaN(lastGain) && hasQ) {
                    result[current].Filters.Add(new PeakingEQ(sampleRate, lastFrequency, lastQ, lastGain));
                    lastGain = double.NaN;
                    lastFrequency = double.NaN;
                    lastQ = double.NaN;
                    hasQ = false;
                }
                lastLine = line;
            }
            return result;
        }

        /// <summary>
        /// Try to parse a gain-only format line (e.g. "31.5 Hz:\t-2.4 dB").
        /// </summary>
        /// <param name="line">Line to parse.</param>
        /// <returns>A tuple of (frequency, gain) if the line matches the gain-only format, otherwise (0, NaN).</returns>
        static (double frequency, double gain) TryParseGainOnlyLine(string line) {
            int hzIndex = line.LastIndexOf(" Hz:", StringComparison.InvariantCulture);
            if (hzIndex <= 0) {
                return (0, double.NaN);
            }
            string freqStr = line[..hzIndex].Trim();
            if (!double.TryParse(freqStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double freq)) {
                return (0, double.NaN);
            }
            int tabAfterHz = line.IndexOf('\t', hzIndex);
            if (tabAfterHz < 0) {
                return (0, double.NaN);
            }
            int dBIndex = line.LastIndexOf(" dB", StringComparison.InvariantCulture);
            if (dBIndex <= tabAfterHz) {
                return (0, double.NaN);
            }
            string gainStr = line[(tabAfterHz + 1)..dBIndex].Trim();
            if (!double.TryParse(gainStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double gain)) {
                return (0, double.NaN);
            }
            return (freq, gain);
        }
    }
}
