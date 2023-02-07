using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Common;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MultEQ-X.
    /// </summary>
    public class MultEQXFilterSet : IIRFilterSet {
        /// <summary>
        /// Extension of the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public override string FileExtension => "mqx";

        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 10;

        /// <summary>
        /// Minimum gain of a single peaking EQ band.
        /// </summary>
        public override double MinGain => -12;

        /// <summary>
        /// Maximum gain of a single peaking EQ band.
        /// </summary>
        public override double MaxGain => 6;

        /// <summary>
        /// This instance is based on a valid configuration file and a modified version can be exported.
        /// </summary>
        public bool Valid { get; private set; } = true;

        /// <summary>
        /// In-file channel GUIDs.
        /// </summary>
        string[] guids;

        /// <summary>
        /// Load a MultEQ-X configuration file for editing.
        /// </summary>
        public static MultEQXFilterSet FromFile(string path) {
            string fileContents = File.ReadAllText(path);
            int pos = fileContents.IndexOf(channelList),
                endPos = -1;
            if (pos != -1) {
                endPos = fileContents.IndexOf(']', pos += channelList.Length);
            }
            if (endPos == -1) {
                throw new CorruptionException("channel list");
            }

            string[] guids = fileContents[pos..endPos].Split(',');
            for (int guid = 0; guid < guids.Length; guid++) {
                pos = guids[guid].IndexOf('"') + 1;
                endPos = guids[guid].LastIndexOf('"');
                if (pos == 0 || endPos == -1 || pos >= endPos) {
                    throw new CorruptionException("guids");
                }
                guids[guid] = guids[guid][pos..endPos];
            }

            ReferenceChannel[] channels = new ReferenceChannel[guids.Length];
            for (int i = 0; i < channels.Length; i++) {
                channels[i] = MultEQMatrix[channels.Length][i];
            }

            return new MultEQXFilterSet(channels, Listener.DefaultSampleRate) {
                guids = guids
            };
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export.
        /// </summary>
        public MultEQXFilterSet(int channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = new string[channels];
            for (int i = 0; i < channels; i++) {
                guids[i] = Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export.
        /// </summary>
        public MultEQXFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = new string[channels.Length];
            for (int i = 0; i < guids.Length; i++) {
                guids[i] = Guid.NewGuid().ToString();
            }
        }

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
            if (!Valid) {
                throw new InvalidSourceException();
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine(fileStart);
            for (int channel = 0; channel < guids.Length;) {
                (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location) label =
                    labeling.FirstOrDefault(x => x.channel == Channels[channel].reference);
                if (label.designation == null) {
                    throw new IOException("A channel that's part of the exported configuration is unsupported by MultEQ-X.");
                }

                result.Append(string.Format(channelEntry, guids[channel], label.designation, label.name,
                    label.pairDesignation, label.pair, label.location,
                    Channels[channel].gain.ToString(CultureInfo.InvariantCulture),
                    GetDelay(channel).ToString(CultureInfo.InvariantCulture),
                    Channels[channel].switchPolarity.ToString().ToLower()));
                if (++channel != guids.Length) {
                    result.AppendLine(",");
                } else {
                    result.AppendLine();
                }
            }

            result.AppendLine(fileBeforeGuids);
            for (int channel = 0; channel < guids.Length;) {
                result.Append('"').Append(guids[channel]).Append('"');
                if (++channel != guids.Length) {
                    result.AppendLine(",");
                } else {
                    result.AppendLine();
                }
            }

            result.AppendLine(fileBeforeTargets);
            for (int channel = 0; channel < guids.Length;) {
                BiquadFilter[] filters = Channels[channel].filters;
                if (filters == null) {
                    continue;
                }
                for (int filter = 0; filter < filters.Length;) {
                    result.Append(string.Format(filterEntry,
                        filters[filter].CenterFreq.ToString(CultureInfo.InvariantCulture),
                        filters[filter].Gain.ToString(CultureInfo.InvariantCulture),
                        filters[filter].Q.ToString(CultureInfo.InvariantCulture),
                        FilterTypeID(filters[filter]),
                        guids[channel]
                    ));
                    if (++filter != filters.Length || ++channel != guids.Length) {
                        result.AppendLine(",");
                    } else {
                        result.AppendLine();
                    }
                }
            }

            result.Append(fileEnd);
            File.WriteAllText(path, result.ToString());
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
        /// Beginning of the JSON file.
        /// </summary>
        const string fileStart = "{ \"_measurements\": [], \"_channelDataMap\": {";

        /// <summary>
        /// A channel's metadata in the MultEQ-X configuration file.
        /// </summary>
        const string channelEntry = @"""{0}"": {{
    ""Metadata"": {{
        ""AvrOriginatingDesignation"": ""{1}"",
        ""DisplayName"": ""{2}"",
        ""PairDesignation"": ""{3}"",
        ""PairDisplayName"": ""{4}"",
        ""Location"": ""{5}""
    }},
    ""Calibration"": {{
        ""IsEnabled"": true,
        ""Trim"": {6},
        ""DistanceMilliseconds"": {7},
        ""PolarityError"": {8},
        ""SpeakerSize"": ""Small"",
        ""CrossoverFrequency"": 80.0
    }},
    ""TargetCurveCutoff"": {{
        ""Mode"": ""Auto""
    }}
}}";

        /// <summary>
        /// The text separating the channels and the GUIDs.
        /// </summary>
        const string fileBeforeGuids = "}, \"OrderedChannelGuids\": [";

        /// <summary>
        /// The text separating the GUIDs and the EQs.
        /// </summary>
        const string fileBeforeTargets = "], \"CalibrationSettings\": { \"AutoTrims\": false, \"AutoDistance\": false, " +
            "\"AutoEnable\": false, \"AutoBassManagement\": false }, \"TargetCurveSet\": [";

        /// <summary>
        /// A filter entry in a MultEQ-X configuration file, prepared for a single channel.
        /// </summary>
        const string filterEntry = @"    {{
      ""_itemString"": ""{{\""Frequency\"":{0},\""Gain\"":{1},\""Q\"":{2},\""Type\"":{3}}}"",
      ""_itemType"": ""Audyssey.CoreData.BiquadData, Audyssey.CoreData, Version=1.4.610.0, Culture=neutral, PublicKeyToken=null"",
      ""Channels"": [ ""{4}"" ], ""All"": false, ""Name"": null, ""ApplyToReference"": true, ""ApplyToFlat"": true
    }}";

        /// <summary>
        /// Closing of the JSON file.
        /// </summary>
        const string fileEnd = "], \"PositionNames\": {}, \"UsedLocalMicrophones\": {} }";

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

        /// <summary>
        /// Values of MultEQ fields for <see cref="ReferenceChannel"/>s.
        /// </summary>
        static readonly (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair,
            string location)[] labeling = {
            (ReferenceChannel.FrontLeft, "FL", "Front Left", "F_", "Front", "FL"),
            (ReferenceChannel.FrontRight, "FR", "Front Right", "F_", "Front", "FR"),
            (ReferenceChannel.FrontCenter, "C", "Center", "C", "Center", "Center, Front"),
            (ReferenceChannel.ScreenLFE, "SW1", "Subwoofer 1", "SW1", "Subwoofer 1", "Subwoofer, Position1"),
            (ReferenceChannel.SideLeft, "SLA", "Surround Left", "S_A", "Surround", "Left, Surround, Position1"),
            (ReferenceChannel.SideRight, "SRA", "Surround Right", "S_A", "Surround", "Right, Surround, Position1"),
            (ReferenceChannel.RearLeft, "SBL", "Surround Back Left", "SB_", "Surround Back", "Left, Back"),
            (ReferenceChannel.RearRight, "SBR", "Surround Back Right", "SB_", "Surround Back", "Right, Back"),
        };
    }
}