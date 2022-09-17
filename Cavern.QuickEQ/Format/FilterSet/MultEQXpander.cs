using Cavern.Filters;
using Cavern.Remapping;
using System.IO;
using System.Text;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Adds a set of <see cref="PeakingEQ"/> filters to selected channels in an Audyssey MultEQ-X configuration file.
    /// </summary>
    public class MultEQXpander { // TODO: add support for gains and delays
        const int highpassEQType = 12;
        const int highShelfEQType = 2;
        const int lowpassEQType = 13;
        const int lowShelfEQType = 3;
        const int peakingEQType = 19;
        const string channelList = "\"OrderedChannelGuids\":";
        const string targetList = "\"TargetCurveSet\"";

        /// <summary>
        /// A filter entry in a MultEQ-X configuration file, prepared for a single channel.
        /// </summary>
        const string entry = @",
    {{
      ""_itemString"": ""{{\""Frequency\"":{0},\""Gain\"":{1},\""Q\"":{2},\""Type\"":{3}}}"",
      ""_itemType"": ""Audyssey.CoreData.BiquadData, Audyssey.CoreData, Version=1.0.350.0, Culture=neutral, PublicKeyToken=null"",
      ""Channels"": [
        ""{4}""
      ],
      ""All"": false,
      ""Name"": null,
      ""ApplyToReference"": true,
      ""ApplyToFlat"": true
    }}";

        static readonly ReferenceChannel[][] MultEQMatrix = new ReferenceChannel[][] {
            new ReferenceChannel[0],
            new ReferenceChannel[1] { ReferenceChannel.FrontCenter },
            new ReferenceChannel[2] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[3] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter },
            new ReferenceChannel[4] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[5] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[6] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.ScreenLFE },
            new ReferenceChannel[7] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.ScreenLFE, ReferenceChannel.ScreenLFE },
            new ReferenceChannel[8] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.ScreenLFE }
        };

        /// <summary>
        /// Translates Cavern filter classes to MultEQ-X filter IDs.
        /// </summary>
        static int FilterTypeID(BiquadFilter filter) {
            if (filter is Highpass) {
                return highpassEQType;
            }
            if (filter is HighShelf) {
                return highShelfEQType;
            }
            if (filter is Lowpass) {
                return lowpassEQType;
            }
            if (filter is LowShelf) {
                return lowShelfEQType;
            }
            if (filter is PeakingEQ) {
                return peakingEQType;
            }
            throw new UnsupportedFilterException();
        }

        /// <summary>
        /// This instance is based on a valid configuration file and a modified version can be exported.
        /// </summary>
        public bool Valid { get; private set; } = true;

        /// <summary>
        /// The entire loaded configuration file.
        /// </summary>
        readonly string fileContents;

        /// <summary>
        /// In-file channel GUIDs.
        /// </summary>
        readonly string[] guids;

        /// <summary>
        /// Applied filter sets for each channel in the configuration file.
        /// </summary>
        readonly BiquadFilter[][] filters;

        /// <summary>
        /// Create an instance of MultEQ-Xpander by loading an existing MultEQ-X configuration file.
        /// </summary>
        public MultEQXpander(string path) {
            fileContents = File.ReadAllText(path);
            int pos = fileContents.IndexOf(channelList),
                endPos = -1;
            if (pos != -1) {
                endPos = fileContents.IndexOf(']', pos += channelList.Length);
            }
            if (endPos == -1) {
                Valid = false;
                return;
            }

            guids = fileContents[pos..endPos].Split(',');
            filters = new BiquadFilter[guids.Length][];
            for (int guid = 0; guid < guids.Length; ++guid) {
                pos = guids[guid].IndexOf('"') + 1;
                endPos = guids[guid].LastIndexOf('"');
                if (pos == -1 || endPos == -1 || pos >= endPos) {
                    Valid = false;
                    return;
                }
                guids[guid] = guids[guid][pos..endPos];
            }
        }

        /// <summary>
        /// Apply a filter set on the target system's selected channel.
        /// </summary>
        public void SetFilters(ReferenceChannel channel, BiquadFilter[] filterSet) {
            for (int i = 0; i < guids.Length; ++i) {
                if (MultEQMatrix[guids.Length][i] == channel) {
                    filters[i] = filterSet;
                }
            }
        }

        /// <summary>
        /// Export the modified version of the loaded configuration file containing all applied filters.
        /// </summary>
        public void Export(string path) {
            if (!Valid)
                throw new InvalidSourceException();
            int pos = fileContents.IndexOf('[', fileContents.IndexOf(targetList) + targetList.Length);
            int level = 0;
            while (level != -1) {
                ++pos;
                if (fileContents[pos] == '[') {
                    ++level;
                } else if (fileContents[pos] == ']') {
                    --level;
                }
            }
            pos = fileContents.LastIndexOf('}', pos) + 1;

            StringBuilder builder = new StringBuilder(fileContents[..pos]);
            for (int channel = 0; channel < guids.Length; ++channel) {
                if (filters[channel] == null) {
                    continue;
                }
                for (int filter = 0; filter < filters[channel].Length; ++filter) {
                    builder.Append(string.Format(entry,
                        filters[channel][filter].CenterFreq.ToString().Replace(',', '.'),
                        filters[channel][filter].Gain.ToString().Replace(',', '.'),
                        filters[channel][filter].Q.ToString().Replace(',', '.'),
                        FilterTypeID(filters[channel][filter]),
                        guids[channel]
                    ));
                }
            }
            builder.Append(fileContents[pos..]);
            File.WriteAllText(path, builder.ToString());
        }
    }
}