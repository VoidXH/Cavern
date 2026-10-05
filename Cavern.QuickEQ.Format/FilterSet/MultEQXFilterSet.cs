using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Exceptions;
using Cavern.Format.FilterSet.Consts;
using Cavern.Format.JSON;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MultEQ-X.
    /// </summary>
    public class MultEQXFilterSet : MultEQXRawFilterSet {
        /// <summary>
        /// Extension of the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public override string FileExtension => "mqx";

        /// <summary>
        /// Whether this filter set supports crossover configuration.
        /// </summary>
        public override bool SupportsCrossover => true;

        /// <summary>
        /// This instance is based on a valid configuration file and a modified version can be exported.
        /// </summary>
        public bool Valid { get; private set; } = true;

        /// <summary>
        /// In-file channel GUIDs.
        /// </summary>
        string[] guids;

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
        /// Load a MultEQ-X configuration file for editing.
        /// </summary>
        public static MultEQXFilterSet FromFile(string path) => FromFile(path, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration file for editing with a target sample rate.
        /// </summary>
        public static MultEQXFilterSet FromFile(string path, int sampleRate) => FromString(File.ReadAllText(path), sampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a string for editing.
        /// </summary>
        public static MultEQXFilterSet FromString(string jsonContent) => FromString(jsonContent, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a string for editing with a target sample rate.
        /// </summary>
        public static MultEQXFilterSet FromString(string jsonContent, int sampleRate) => FromJson(new JsonFile(jsonContent), sampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a parsed JSON file for editing.
        /// </summary>
        public static MultEQXFilterSet FromJson(JsonFile json) => FromJson(json, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a parsed JSON file for editing with a target sample rate.
        /// </summary>
        public static MultEQXFilterSet FromJson(JsonFile json, int sampleRate) {
            Dictionary<string, ReferenceChannel> channelMapping = MQXHelpers.GetChannelMapping(json);
            if (channelMapping == null || !json.ContainsKey(channelListKey)) {
                throw new CorruptionException("channel list");
            }

            object[] orderedGuids = (object[])json[channelListKey];
            string[] sourceGuids = new string[orderedGuids.Length];
            ReferenceChannel[] channels = new ReferenceChannel[orderedGuids.Length];
            for (int i = 0; i < orderedGuids.Length; i++) {
                string guid = (string)orderedGuids[i];
                sourceGuids[i] = guid;
                if (!channelMapping.TryGetValue(guid, out channels[i])) {
                    channels[i] = ReferenceChannel.Unknown;
                }
            }

            return new MultEQXFilterSet(channels, sampleRate) {
                guids = sourceGuids
            };
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
            File.WriteAllText(path, Export(false));
        }

        /// <summary>
        /// Generate the full MultEQ-X JSON configuration containing all applied filters.
        /// </summary>
        protected override string Export(bool gainOnly) {
            if (!Valid) {
                throw new InvalidSourceException();
            }

            double[] gains = GetGains(-12, 12);
            double[] delays = GetDelays(20);

            JsonFile channelDataMap = new JsonFile();
            for (int channel = 0; channel < guids.Length; channel++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[channel];
                (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location) label =
                    MQXConsts.labeling.FirstOrDefault(x => x.channel == channelRef.reference);
                if (label.designation == null) {
                    throw new IOException("A channel that's part of the exported configuration is unsupported by MultEQ-X.");
                }

                channelDataMap[guids[channel]] = new JsonFile {
                    {
                        "Metadata", new JsonFile {
                            { "AvrOriginatingDesignation", label.designation },
                            { "DisplayName", label.name },
                            { "PairDesignation", label.pairDesignation },
                            { "PairDisplayName", label.pair },
                            { "Location", label.location }
                        }
                    },
                    {
                        "Calibration", new JsonFile {
                            { "IsEnabled", true },
                            { "Trim", gains[channel] },
                            { "DistanceMilliseconds", delays[channel] },
                            { "PolarityError", channelRef.switchPolarity },
                            { "SpeakerSize", "Small" },
                            { "CrossoverFrequency", channelRef.crossoverFrequency ?? 80 }
                        }
                    },
                    {
                        "TargetCurveCutoff", new JsonFile {
                            { "Mode", "Auto" }
                        }
                    }
                };
            }

            object[] orderedGuids = new object[guids.Length];
            for (int i = 0; i < guids.Length; i++) {
                orderedGuids[i] = guids[i];
            }

            List<object> targetCurves = new List<object>();
            for (int channel = 0; channel < guids.Length; channel++) {
                BiquadFilter[] filters = ((IIRChannelData)Channels[channel]).filters;
                if (filters == null) {
                    continue;
                }
                for (int filter = 0; filter < filters.Length; filter++) {
                    JsonFile item = new JsonFile {
                        { "Frequency", filters[filter].CenterFreq },
                        { "Gain", filters[filter].Gain },
                        { "Q", filters[filter].Q },
                        { "Type", FilterTypeID(filters[filter]) }
                    };

                    targetCurves.Add(new JsonFile {
                        { "_itemString", item.ToString() },
                        { "_itemType", filterItemType },
                        { "Channels", new object[] { guids[channel] } },
                        { "All", false },
                        { "Name", null },
                        { "ApplyToReference", true },
                        { "ApplyToFlat", true }
                    });
                }
            }

            return new JsonFile {
                { "_measurements", Array.Empty<object>() },
                { MQXConsts.channelMappingKey, channelDataMap },
                { channelListKey, orderedGuids },
                { "CalibrationSettings", new JsonFile {
                    { "AutoTrims", false },
                    { "AutoDistance", false },
                    { "AutoEnable", false },
                    { "AutoBassManagement", false }
                } },
                { "TargetCurveSet", targetCurves.ToArray() },
                { "PositionNames", new JsonFile() },
                { "UsedLocalMicrophones", new JsonFile() }
            }.ToString();
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
        const string channelListKey = "OrderedChannelGuids";

        /// <summary>
        /// Assembly-qualified type name for biquad filter items in MultEQ-X.
        /// </summary>
        const string filterItemType = "Audyssey.CoreData.BiquadData, Audyssey.CoreData, Version=1.4.610.0, Culture=neutral, PublicKeyToken=null";
    }
}
