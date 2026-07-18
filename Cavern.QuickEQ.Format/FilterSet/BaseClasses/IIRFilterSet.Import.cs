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
        public IIRFilterSet(string contents, int sampleRate) : base(sampleRate) {
            List<(string Name, List<PeakingEQ> Filters)> parsed = ParseExport(contents, sampleRate);
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
        /// </summary>
        /// <param name="contents">Text created by <see cref="Export()"/>.</param>
        /// <param name="sampleRate">Sample rate used to construct the parsed <see cref="PeakingEQ"/> filters.</param>
        /// <returns>Channel name to its parsed peaking EQ filter set, in section order</returns>
        static List<(string Name, List<PeakingEQ> Filters)> ParseExport(string contents, int sampleRate) {
            List<(string Name, List<PeakingEQ> Filters)> result = new List<(string, List<PeakingEQ>)>();
            string lastLine = string.Empty;
            int current = -1;
            double lastGain = double.NaN;
            double lastFrequency = double.NaN;
            double lastQ = double.NaN;

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
                }

                if (current >= 0 && !double.IsNaN(lastFrequency) && !double.IsNaN(lastGain) && !double.IsNaN(lastQ)) {
                    result[current].Filters.Add(new PeakingEQ(sampleRate, lastFrequency, lastQ, lastGain));
                    lastGain = double.NaN;
                    lastFrequency = double.NaN;
                    lastQ = double.NaN;
                }
                lastLine = line;
            }
            return result;
        }
    }
}
