using System.Collections.Generic;
using System.Linq;

using Cavern.Filters;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet.Special {
    /// <summary>
    /// Loads files created with different versions of <see cref="IIRFilterSet.Export()"/>.
    /// </summary>
    public class ParsedIIRFilterSet : IIRFilterSet {
        /// <summary>
        /// Each named channel's filters.
        /// </summary>
        public Dictionary<string, IReadOnlyList<PeakingEQ>> Filters { get; private set; } = new Dictionary<string, IReadOnlyList<PeakingEQ>>();

        /// <summary>
        /// Loads a single-file export created with <see cref="IIRFilterSet.Export()"/>.
        /// </summary>
        public ParsedIIRFilterSet(string contents) : base(CountChannels(contents), Listener.DefaultSampleRate) {
            string lastLine = string.Empty;
            string lastChannel = string.Empty;
            double lastGain = double.NaN;
            double lastFrequency = double.NaN;
            double lastQ = double.NaN;
            foreach (string line in contents.ReadLines()) {
                if (line.IsTheSameCharacter() == '=') {
                    lastChannel = lastLine;
                } else if (line.StartsWith(GainLineStart)) {
                    lastGain = QMath.ParseDouble(line.ReadUntil(GainLineStart.Length, ' '));
                } else if (line.StartsWith(FrequencyLineStart)) {
                    lastFrequency = QMath.ParseDouble(line.ReadUntil(FrequencyLineStart.Length, ' '));
                } else if (line.StartsWith(QLineStart)) {
                    lastQ = QMath.ParseDouble(line.ReadUntil(QLineStart.Length, ' '));
                }

                if (!double.IsNaN(lastFrequency) && !double.IsNaN(lastGain) && !double.IsNaN(lastQ)) {
                    PeakingEQ filter = new PeakingEQ(SampleRate, lastFrequency, lastQ, lastGain);
                    if (Filters.TryGetValue(lastChannel, out IReadOnlyList<PeakingEQ> list)) {
                        ((List<PeakingEQ>)list).Add(filter);
                    } else {
                        list = new List<PeakingEQ> {
                            filter
                        };
                        Filters[lastChannel] = list;
                    }

                    lastGain = double.NaN;
                    lastFrequency = double.NaN;
                    lastQ = double.NaN;
                }
                lastLine = line;
            }

            int channel = 0;
            foreach (KeyValuePair<string, IReadOnlyList<PeakingEQ>> filter in Filters) {
                IIRChannelData channelRef = (IIRChannelData)Channels[channel++];
                channelRef.name = filter.Key;
                channelRef.filters = filter.Value.ToArray();
            }
        }

        /// <summary>
        /// Because the base constructor needs the channel count, a channel-counter pass is required.
        /// </summary>
        static int CountChannels(string contents) {
            int result = 0;
            foreach (string line in contents.ReadLines()) {
                if (line.IsTheSameCharacter() == '=') {
                    result++;
                }
            }
            return result;
        }
    }
}
