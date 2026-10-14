using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Channels;
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
            guids = MQXHelpers.GenerateGuids(channels);
            sourceData = null;
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export.
        /// </summary>
        public MultEQXFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = MQXHelpers.GenerateGuids(channels.Length);
            sourceData = null;
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export from fully spelled out channel names.
        /// </summary>
        /// <param name="channelNames">Display names resolved from the MQX file's DisplayName field</param>
        /// <param name="sampleRate">Target sample rate</param>
        public MultEQXFilterSet(string[] channelNames, int sampleRate) : base(MQXHelpers.GetReferenceChannels(channelNames), sampleRate) {
            Valid = true;
            guids = MQXHelpers.GenerateGuids(channelNames.Length);
            sourceData = null;
            if (channelNames != null) {
                for (int i = 0; i < Channels.Length; i++) {
                    if (!string.IsNullOrEmpty(channelNames[i])) {
                        ((EqualizerChannelData)Channels[i]).name = channelNames[i];
                    }
                }
            }
        }

        /// <summary>
        /// Create a MultEQ-X configuration file from a parsed MQX document.
        /// </summary>
        /// <param name="json">Parsed MQX data</param>
        /// <param name="sampleRate">Target sample rate</param>
        public static MultEQXFilterSet FromJson(JsonFile json, int sampleRate) {
            (string guid, string name, ReferenceChannel reference)[] channels = MQXHelpers.GetOrderedChannels(json);

            MultEQXFilterSet result = new MultEQXFilterSet(channels.SelectArray(x => x.name), sampleRate) {
                guids = channels.SelectArray(x => x.guid)
            };
            for (int i = 0; i < result.Channels.Length; i++) {
                result.Channels[i].name = channels[i].name ?? channels[i].ToString();
                result.Channels[i].reference = channels[i].reference;
            }
            return result;
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
            File.WriteAllText(path, GetExportData().ToString());
        }

        /// <summary>
        /// Export the filter set as a JSON string.
        /// </summary>
        public string Export() {
            return GetExportData().ToString();
        }

        /// <summary>
        /// Export the preloaded MQX document to a target stream.
        /// </summary>
        /// <param name="stream">Target stream</param>
        public override void Export(Stream stream) {
            using StreamWriter writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024, true);
            writer.Write(GetExportData().ToString());
        }

        /// <summary>
        /// Create a new MultEQ-X configuration from scratch.
        /// </summary>
        JsonFile CreateNewConfiguration() => MQXHelpers.CreateConfiguration(CreateChannelDataMap(), guids, CreateTargetCurves());

        /// <summary>
        /// Apply the filter set to a parsed MultEQ-X MQX document in memory.
        /// </summary>
        /// <param name="data">Parsed MQX JSON tree</param>
        void ApplyExport(JsonFile data) => MQXHelpers.ApplyToDocument(data, CreateChannelDataMap(), guids, CreateTargetCurves());

        /// <summary>
        /// Build the target curve elements for every channel that has an equalizer curve.
        /// </summary>
        object[] CreateTargetCurves() {
            List<object> targetCurves = new List<object>();
            for (int channel = 0; channel < guids.Length; channel++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                Equalizer equalizer = channelRef.curve;
                if (equalizer == null || equalizer.Bands.Count == 0) {
                    continue;
                }

                string name = MQXConsts.labeling.FirstOrDefault(x => x.channel == channelRef.reference).name;
                JsonFile curveData = new JsonFile {
                    { "Points", CreateCurvePoints(equalizer) },
                    { "DisplayName", name }
                };

                targetCurves.Add(new JsonFile {
                    { "_itemString", curveData.ToString() },
                    { "_itemType", customCurveItemType },
                    { "Channels", new object[] { guids[channel] } },
                    { "ExcludedChannels", Array.Empty<object>() },
                    { "All", false },
                    { "Name", name },
                    { "ApplyToReference", true },
                    { "ApplyToFlat", true }
                });
            }

            return targetCurves.ToArray();
        }

        /// <summary>
        /// Build the full MQX document to export, either from scratch or by applying the filters to the loaded source.
        /// </summary>
        JsonFile GetExportData() {
            if (sourceData == null) {
                return CreateNewConfiguration();
            }

            JsonFile data = new JsonFile(sourceData.ToString());
            ApplyExport(data);
            return data;
        }

        /// <summary>
        /// Build the per-channel data map element of the exported MQX document.
        /// </summary>
        JsonFile CreateChannelDataMap() {
            bool[] polarities = new bool[Channels.Length];
            for (int i = 0; i < Channels.Length; i++) {
                polarities[i] = ((EqualizerChannelData)Channels[i]).switchPolarity;
            }
            return MQXHelpers.CreateChannelDataMap(guids, Channels, polarities, GetGains(-12, 12), GetDelays(20));
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
        /// Assembly-qualified type name for custom curve items in MultEQ-X.
        /// </summary>
        const string customCurveItemType = "Audyssey.CoreData.CustomCurveData, Audyssey.CoreData, Version=1.8.873.0, Culture=neutral, PublicKeyToken=null";
    }
}
