using System.Globalization;
using System.IO;
using System.Text;

using Cavern.Filters;
using Cavern.Remapping;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Adds a set of <see cref="PeakingEQ"/> filters to selected channels in an Audyssey MultEQ-X configuration file.
    /// </summary>
    // TODO: add support for gains and delays
    public class MultEQXpander : IIRFilterSet {
        /// <summary>
        /// This instance is based on a valid configuration file and a modified version can be exported.
        /// </summary>
        public bool Valid { get; private set; } = true;

        /// <summary>
        /// The entire loaded configuration file.
        /// </summary>
        string fileContents;

        /// <summary>
        /// In-file channel GUIDs.
        /// </summary>
        string[] guids;

        /// <summary>
        /// Create an instance of MultEQ-Xpander by loading an existing MultEQ-X configuration file.
        /// </summary>
        public MultEQXpander(string path) : base(path) { }

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
        /// Export the modified version of the loaded configuration file containing all applied filters.
        /// </summary>
        public override void Export(string path) {
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
                if (Filters[channel] == null) {
                    continue;
                }
                for (int filter = 0; filter < Filters[channel].Length; ++filter) {
                    builder.Append(string.Format(entry,
                        Filters[channel][filter].CenterFreq.ToString(CultureInfo.InvariantCulture),
                        Filters[channel][filter].Gain.ToString(CultureInfo.InvariantCulture),
                        Filters[channel][filter].Q.ToString(CultureInfo.InvariantCulture),
                        FilterTypeID(Filters[channel][filter]),
                        guids[channel]
                    ));
                }
            }
            builder.Append(fileContents[pos..]);
            File.WriteAllText(path, builder.ToString());
        }

        /// <summary>
        /// Open a MultEQ-X configuration for editing.
        /// </summary>
        protected override ReferenceChannel[] ReadFile(string path) {
            fileContents = File.ReadAllText(path);
            int pos = fileContents.IndexOf(channelList),
                endPos = -1;
            if (pos != -1) {
                endPos = fileContents.IndexOf(']', pos += channelList.Length);
            }
            if (endPos == -1) {
                Valid = false;
                return MultEQMatrix[0];
            }

            guids = fileContents[pos..endPos].Split(',');
            for (int guid = 0; guid < guids.Length; ++guid) {
                pos = guids[guid].IndexOf('"') + 1;
                endPos = guids[guid].LastIndexOf('"');
                if (pos == 0 || endPos == -1 || pos >= endPos) {
                    Valid = false;
                    return MultEQMatrix[0];
                }
                guids[guid] = guids[guid][pos..endPos];
            }

            return MultEQMatrix[guids.Length];
        }

        /// <summary>
        /// Filter type ID of a highpass.
        /// </summary>
        const int highpassEQType = 12;

        /// <summary>
        /// Filter type ID of a high-shelf.
        /// </summary>
        const int highShelfEQType = 2;

        /// <summary>
        /// Filter type ID of a lowpass.
        /// </summary>
        const int lowpassEQType = 13;

        /// <summary>
        /// Filter type ID of a low-shelf.
        /// </summary>
        const int lowShelfEQType = 3;

        /// <summary>
        /// Filter type ID of a peaking EQ.
        /// </summary>
        const int peakingEQType = 19;

        /// <summary>
        /// JSON tag for the list of channels.
        /// </summary>
        const string channelList = "\"OrderedChannelGuids\":";

        /// <summary>
        /// JSON tag for the list of target curves.
        /// </summary>
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

        /// <summary>
        /// Channel layout for each channel count in a MultEQ-X configuration file.
        /// </summary>
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
    }
}