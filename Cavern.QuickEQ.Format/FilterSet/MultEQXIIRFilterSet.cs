using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Exceptions;
using Cavern.Format.FilterSet.Consts;
using Cavern.Format.JSON;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MultEQ-X with peaking EQ.
    /// </summary>
    public class MultEQXIIRFilterSet : MultEQXRawFilterSet {
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
        public MultEQXIIRFilterSet(int channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = MQXHelpers.GenerateGuids(channels);
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export.
        /// </summary>
        public MultEQXIIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) {
            Valid = true;
            guids = MQXHelpers.GenerateGuids(channels.Length);
        }

        /// <summary>
        /// Create a MultEQ-X configuration file for EQ export from fully spelled out channel names.
        /// </summary>
        /// <param name="channelNames">Display names resolved from the MQX file's DisplayName field</param>
        /// <param name="sampleRate">Target sample rate</param>
        public MultEQXIIRFilterSet(string[] channelNames, int sampleRate) : base(MQXHelpers.GetReferenceChannels(channelNames), sampleRate) {
            Valid = true;
            guids = MQXHelpers.GenerateGuids(channelNames.Length);
            if (channelNames != null) {
                for (int i = 0; i < Channels.Length; i++) {
                    if (!string.IsNullOrEmpty(channelNames[i])) {
                        ((IIRChannelData)Channels[i]).name = channelNames[i];
                    }
                }
            }
        }

        /// <summary>
        /// Load a MultEQ-X configuration file for editing.
        /// </summary>
        public static MultEQXIIRFilterSet FromFile(string path) => FromFile(path, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration file for editing with a target sample rate.
        /// </summary>
        public static MultEQXIIRFilterSet FromFile(string path, int sampleRate) => FromString(File.ReadAllText(path), sampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a string for editing.
        /// </summary>
        public static MultEQXIIRFilterSet FromString(string jsonContent) => FromString(jsonContent, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a string for editing with a target sample rate.
        /// </summary>
        public static MultEQXIIRFilterSet FromString(string jsonContent, int sampleRate) => FromJson(new JsonFile(jsonContent), sampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a parsed JSON file for editing.
        /// </summary>
        public static MultEQXIIRFilterSet FromJson(JsonFile json) => FromJson(json, Listener.DefaultSampleRate);

        /// <summary>
        /// Load a MultEQ-X configuration from a parsed JSON file for editing with a target sample rate.
        /// </summary>
        public static MultEQXIIRFilterSet FromJson(JsonFile json, int sampleRate) {
            (string guid, string name, ReferenceChannel reference)[] channels = MQXHelpers.GetOrderedChannels(json);

            MultEQXIIRFilterSet result = new MultEQXIIRFilterSet(channels.SelectArray(x => x.name), sampleRate) {
                guids = channels.SelectArray(x => x.guid)
            };
            for (int i = 0; i < result.Channels.Length; i++) {
                result.Channels[i].name = channels[i].name ?? channels[i].ToString();
                result.Channels[i].reference = channels[i].reference;
            }
            return result;
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
        public override void Export(string path) => File.WriteAllText(path, Export(false));

        /// <summary>
        /// Generate the full MultEQ-X JSON configuration containing all applied filters.
        /// </summary>
        protected override string Export(bool gainOnly) {
            if (!Valid) {
                throw new InvalidSourceException();
            }

            JsonFile channelDataMap = CreateChannelDataMap();

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

            return MQXHelpers.CreateConfiguration(channelDataMap, guids, targetCurves.ToArray()).ToString();
        }

        /// <summary>
        /// Build the per-channel data map element of the exported MQX document.
        /// </summary>
        JsonFile CreateChannelDataMap() {
            bool[] polarities = new bool[Channels.Length];
            for (int i = 0; i < Channels.Length; i++) {
                polarities[i] = ((IIRChannelData)Channels[i]).switchPolarity;
            }
            return MQXHelpers.CreateChannelDataMap(guids, Channels, polarities, GetGains(-12, 12), GetDelays(20));
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
        /// Assembly-qualified type name for biquad filter items in MultEQ-X.
        /// </summary>
        const string filterItemType = "Audyssey.CoreData.BiquadData, Audyssey.CoreData, Version=1.4.610.0, Culture=neutral, PublicKeyToken=null";
    }
}