using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Format.Exceptions;
using Cavern.Format.FilterSet.Consts;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Equalizer filter set for MultEQ-X using custom target curves.
    /// </summary>
    public class MultEQXFilterSet : EqualizerFilterSet {
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
        /// Parsed MQX data used for stream exports.
        /// </summary>
        readonly JsonFile sourceData;

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export.
        /// </summary>
        public MultEQXFilterSet(int channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = new string[channels];
            for (int i = 0; i < channels; i++) {
                guids[i] = Guid.NewGuid().ToString();
            }
            sourceData = null;
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
            sourceData = null;
        }

        /// <summary>
        /// Create a MultEQ-X configuration file from a parsed MQX document.
        /// </summary>
        /// <param name="data">Parsed MQX data</param>
        /// <param name="sampleRate">Target sample rate</param>
        public static MultEQXFilterSet FromJson(JsonFile data, int sampleRate) {
            Dictionary<string, ReferenceChannel> channelMapping = MQXHelpers.GetChannelMapping(data);
            if (channelMapping == null || !data.ContainsKey(channelListKey)) {
                throw new CorruptionException("channel list");
            }

            object[] orderedGuids = (object[])data[channelListKey];
            string[] sourceGuids = new string[orderedGuids.Length];
            ReferenceChannel[] channels = new ReferenceChannel[orderedGuids.Length];
            for (int i = 0; i < orderedGuids.Length; i++) {
                string guid = (string)orderedGuids[i];
                sourceGuids[i] = guid;
                if (!channelMapping.TryGetValue(guid, out channels[i])) {
                    channels[i] = ReferenceChannel.Unknown;
                }
            }

            return new MultEQXFilterSet(channels, sampleRate, data) {
                guids = sourceGuids
            };
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
        /// Create a MultEQ-X configuration file from a parsed MQX document.
        /// </summary>
        /// <param name="channels">Reference channels in export order</param>
        /// <param name="sampleRate">Target sample rate</param>
        /// <param name="data">Parsed MQX data</param>
        MultEQXFilterSet(ReferenceChannel[] channels, int sampleRate, JsonFile data) : base(channels, sampleRate) {
            sourceData = new JsonFile(data.ToString());
        }

        /// <summary>
        /// Export the modified version of the loaded configuration file containing all applied filters.
        /// </summary>
        public override void Export(string path) {
            JsonFile data;
            if (sourceData == null) {
                data = CreateNewConfiguration();
            } else {
                data = new JsonFile(sourceData.ToString());
                ApplyExport(data);
            }
            File.WriteAllText(path, data.ToString());
        }

        /// <summary>
        /// Export the filter set as a JSON string.
        /// </summary>
        public string Export() {
            JsonFile data;
            if (sourceData == null) {
                data = CreateNewConfiguration();
            } else {
                data = new JsonFile(sourceData.ToString());
                ApplyExport(data);
            }
            return data.ToString();
        }

        /// <summary>
        /// Export the preloaded MQX document to a target stream.
        /// </summary>
        /// <param name="stream">Target stream</param>
        public override void Export(Stream stream) {
            JsonFile data;
            if (sourceData == null) {
                data = CreateNewConfiguration();
            } else {
                data = new JsonFile(sourceData.ToString());
                ApplyExport(data);
            }

            using StreamWriter writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024, true);
            writer.Write(data.ToString());
        }

        /// <summary>
        /// Create a new MultEQ-X configuration from scratch.
        /// </summary>
        JsonFile CreateNewConfiguration() {
            double[] gains = GetGains(-12, 12);
            double[] delays = GetDelays(20);

            JsonFile channelDataMap = new JsonFile();
            (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location)[] labels = new (ReferenceChannel, string, string, string, string, string)[guids.Length];
            for (int channel = 0; channel < guids.Length; channel++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                labels[channel] = MQXConsts.labeling.FirstOrDefault(x => x.channel == channelRef.reference);
                if (labels[channel].designation == null) {
                    throw new IOException("A channel that's part of the exported configuration is unsupported by MultEQ-X.");
                }

                channelDataMap[guids[channel]] = new JsonFile {
                    {
                        "Metadata", new JsonFile {
                            { "AvrOriginatingDesignation", labels[channel].designation },
                            { "DisplayName", labels[channel].name },
                            { "PairDesignation", labels[channel].pairDesignation },
                            { "PairDisplayName", labels[channel].pair },
                            { "Location", labels[channel].location }
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
                            { "Mode", channelRef.reference == ReferenceChannel.ScreenLFE ? "Disabled" : "Auto" }
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
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                Equalizer equalizer = channelRef.curve;
                if (equalizer == null || equalizer.Bands.Count == 0) {
                    continue;
                }

                JsonFile curveData = new JsonFile {
                    { "Points", CreateCurvePoints(equalizer) },
                    { "DisplayName", labels[channel].name }
                };

                targetCurves.Add(new JsonFile {
                    { "_itemString", curveData.ToString() },
                    { "_itemType", customCurveItemType },
                    { "Channels", new object[] { guids[channel] } },
                    { "ExcludedChannels", Array.Empty<object>() },
                    { "All", false },
                    { "Name", labels[channel].name },
                    { "ApplyToReference", true },
                    { "ApplyToFlat", true }
                });
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
            };
        }

        /// <summary>
        /// Apply the filter set to a parsed MultEQ-X MQX document in memory.
        /// </summary>
        /// <param name="data">Parsed MQX JSON tree</param>
        void ApplyExport(JsonFile data) {
            double[] gains = GetGains(-12, 12);
            double[] delays = GetDelays(20);

            JsonFile channelDataMap = new JsonFile();
            (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location)[] labels =
                new (ReferenceChannel, string, string, string, string, string)[guids.Length];
            for (int channel = 0; channel < guids.Length; channel++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                labels[channel] = MQXConsts.labeling.FirstOrDefault(x => x.channel == channelRef.reference);
                if (labels[channel].designation == null) {
                    throw new IOException("A channel that's part of the exported configuration is unsupported by MultEQ-X.");
                }

                channelDataMap[guids[channel]] = new JsonFile {
                    {
                        "Metadata", new JsonFile {
                            { "AvrOriginatingDesignation", labels[channel].designation },
                            { "DisplayName", labels[channel].name },
                            { "PairDesignation", labels[channel].pairDesignation },
                            { "PairDisplayName", labels[channel].pair },
                            { "Location", labels[channel].location }
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
                            { "Mode", channelRef.reference == ReferenceChannel.ScreenLFE ? "Disabled" : "Auto" }
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
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                Equalizer equalizer = channelRef.curve;
                if (equalizer == null || equalizer.Bands.Count == 0) {
                    continue;
                }

                JsonFile curveData = new JsonFile {
                    { "Points", CreateCurvePoints(equalizer) },
                    { "DisplayName", labels[channel].name }
                };

                targetCurves.Add(new JsonFile {
                    { "_itemString", curveData.ToString() },
                    { "_itemType", customCurveItemType },
                    { "Channels", new object[] { guids[channel] } },
                    { "ExcludedChannels", Array.Empty<object>() },
                    { "All", false },
                    { "Name", labels[channel].name },
                    { "ApplyToReference", true },
                    { "ApplyToFlat", true }
                });
            }

            data[MQXConsts.channelMappingKey] = channelDataMap;
            data[channelListKey] = orderedGuids;
            data["TargetCurveSet"] = targetCurves.ToArray();
        }

        /// <summary>
        /// Get the delay of each channel in milliseconds, and confine them to the limits of the output format.
        /// The longer end's relative differences will be kept, as the remaining channels are likely subwoofers.
        /// </summary>
        double[] GetDelays(double maxDelay) {
            double[] result = new double[Channels.Length];
            double max = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                result[i] = GetDelay(i);
                if (max < result[i]) {
                    max = result[i];
                }
            }

            max = Math.Max(max - maxDelay, 0);
            for (int i = 0; i < result.Length; i++) {
                result[i] = Math.Max(result[i] - max, 0);
            }

            return result;
        }

        /// <summary>
        /// Build the exported custom target curve points for a channel.
        /// </summary>
        object[] CreateCurvePoints(Equalizer equalizer) {
            if (equalizer == null || equalizer.Bands.Count == 0) {
                return Array.Empty<object>();
            }

            object[] result = new object[equalizer.Bands.Count];

            for (int i = 0; i < equalizer.Bands.Count; i++) {
                string freq = equalizer.Bands[i].Frequency.ToString(CultureInfo.InvariantCulture);
                string gain = QMath.DbToGain(equalizer.Bands[i].Gain).ToString(CultureInfo.InvariantCulture);
                result[i] = new JsonFile {
                    { "X", freq },
                    { "Y", gain }
                };
            }

            return result;
        }

        /// <summary>
        /// JSON tag for the list of channels.
        /// </summary>
        const string channelListKey = "OrderedChannelGuids";

        /// <summary>
        /// Assembly-qualified type name for custom curve items in MultEQ-X.
        /// </summary>
        const string customCurveItemType = "Audyssey.CoreData.CustomCurveData, Audyssey.CoreData, Version=1.8.873.0, Culture=neutral, PublicKeyToken=null";
    }
}
