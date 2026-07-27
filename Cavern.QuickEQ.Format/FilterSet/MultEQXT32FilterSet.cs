using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Cavern.Channels;
using Cavern.Format.Exceptions;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Equalizer filter set for MultEQ XT32.
    /// </summary>
    public class MultEQXT32FilterSet : EqualizerFilterSet {
        /// <summary>
        /// Extension of the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public override string FileExtension => "ady";

        /// <summary>
        /// Parsed ADY data used for stream exports.
        /// </summary>
        JsonFile sourceData;

        /// <summary>
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(int channels, int sampleRate) : base(channels, sampleRate) {
            sourceData = null;
        }

        /// <summary>
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) {
            sourceData = null;
        }

        /// <summary>
        /// Create a MultEQ XT32 configuration file from a parsed ADY document.
        /// </summary>
        /// <param name="data">Parsed ADY data</param>
        /// <param name="sampleRate">Target sample rate</param>
        public static MultEQXT32FilterSet FromJson(JsonFile data, int sampleRate) {
            object[] detectedChannels = (object[])data["detectedChannels"];
            if (detectedChannels.Length == 0) {
                throw new CorruptionException("No channels found in ADY file");
            }

            ReferenceChannel[] channels = new ReferenceChannel[detectedChannels.Length];
            for (int i = 0; i < channels.Length; i++) {
                JsonFile channelData = (JsonFile)detectedChannels[i];
                channels[i] = MapReference(channelData["commandId"] as string);
            }

            return new MultEQXT32FilterSet(channels, sampleRate, data);
        }

        /// <summary>
        /// Create a MultEQ XT32 configuration file from a parsed ADY document.
        /// </summary>
        /// <param name="channels">Reference channels in export order</param>
        /// <param name="sampleRate">Target sample rate</param>
        /// <param name="data">Parsed ADY data</param>
        MultEQXT32FilterSet(ReferenceChannel[] channels, int sampleRate, JsonFile data) : base(channels, sampleRate) {
            sourceData = new JsonFile(data.ToString());
        }

        /// <summary>
        /// Export the filter set to a MultEQ XT32 ADY file.
        /// </summary>
        /// <param name="path">Target file path</param>
        public override void Export(string path) {
            JsonFile data = new JsonFile(File.ReadAllText(path));
            ApplyExport(data);

            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);

            File.WriteAllText(Path.Combine(folder, $"{fileNameBase} modified.ady"), data.ToString());
        }

        /// <summary>
        /// Export the preloaded ADY document to a target stream.
        /// </summary>
        /// <param name="stream">Target stream</param>
        public override void Export(Stream stream) {
            if (sourceData == null) {
                throw new InvalidSourceException();
            }

            JsonFile data = new JsonFile(sourceData.ToString());
            ApplyExport(data);

            using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            writer.Write(data.ToString());
        }

        /// <summary>
        /// Apply the filter set to a parsed MultEQ XT32 ADY document in memory.
        /// </summary>
        /// <param name="data">Parsed ADY JSON tree</param>
        void ApplyExport(JsonFile data) {
            object[] detectedChannels = (object[])data["detectedChannels"];
            if (detectedChannels.Length == 0) {
                throw new CorruptionException("No channels found in ADY file");
            }
            data["title"] = "Cavern QuickEQ";
            data["dynamicEq"] = false;
            data["dynamicVolume"] = false;
            data["lfc"] = false;
            data["enTargetCurveType"] = 1; // No HF rolloff I think

            double[] gains = GetGains(-12, 12);
            int subwooferIndex = 0;
            for (int i = 0; i < detectedChannels.Length; i++) {
                JsonFile channelData = (JsonFile)detectedChannels[i];
                string commandId = (string)channelData["commandId"];
                ReferenceChannel refChannel = MapReference(commandId);

                int eqIndex;
                if (refChannel == ReferenceChannel.ScreenLFE) {
                    eqIndex = FindNthChannel(ReferenceChannel.ScreenLFE, subwooferIndex);
                    subwooferIndex++;
                } else {
                    eqIndex = Array.FindIndex(Channels, x => x.reference == refChannel);
                }

                if (eqIndex == -1) {
                    continue;
                }
                EqualizerChannelData equalizerChannel = (EqualizerChannelData)Channels[eqIndex];
                JsonFile channelReport = GetOrCreateObject(channelData, "channelReport");

                bool isSub = equalizerChannel.reference == ReferenceChannel.ScreenLFE;

                decimal level = (decimal)Math.Round(gains[eqIndex], 1);
                double distanceMeters = Math.Min(GetDelay(eqIndex), maxDelayMs) * Source.SpeedOfSound / 1000.0;
                decimal distance = (decimal)Math.Round(distanceMeters, 2);

                channelData["delayAdjustment"] = "0.0";
                channelData["trimAdjustment"] = "0.0";
                channelData["customDistance"] = distance;
                channelData["customLevel"] = level.ToString("0.0", CultureInfo.InvariantCulture);
                channelData["frequencyRangeRolloff"] = isSub ? 250 : 20000;
                channelData["customTargetCurvePoints"] = CreateCurve(equalizerChannel.curve);

                channelReport["distance"] = distance;
                channelReport["enSpeakerConnect"] = 1;
                channelReport["customEnSpeakerConnect"] = 1;
                channelReport["isReversePolarity"] = equalizerChannel.switchPolarity;

                if (!isSub) {
                    channelData["midrangeCompensation"] = false;
                    channelData["customSpeakerType"] = "S";
                    channelData["customCrossover"] = "80";
                }
            }
        }

        /// <summary>
        /// Build the exported custom target curve points for a channel.
        /// </summary>
        object[] CreateCurve(Equalizer equalizer) {
            if (equalizer == null || equalizer.Bands.Count == 0) {
                return Array.Empty<string>();
            }

            string[] result = new string[equalizer.Bands.Count];

            for (int i = 0; i < equalizer.Bands.Count; i++) {
                string freq = equalizer.Bands[i].Frequency.ToString(CultureInfo.InvariantCulture);
                string gain = equalizer.Bands[i].Gain.ToString(CultureInfo.InvariantCulture);
                result[i] = $"{{{freq},{gain}}}";
            }

            return result;
        }

        static ReferenceChannel MapReference(string commandId) {
            if (string.IsNullOrEmpty(commandId)) {
                return ReferenceChannel.Unknown;
            }

            string sanitized = new string(commandId.TakeWhile(c => !char.IsDigit(c)).ToArray()).ToUpperInvariant();
            return sanitized switch {
                "FL" => ReferenceChannel.FrontLeft,
                "FR" => ReferenceChannel.FrontRight,
                "C" => ReferenceChannel.FrontCenter,
                "SW" => ReferenceChannel.ScreenLFE,
                "SLA" => ReferenceChannel.SideLeft,
                "SL" => ReferenceChannel.SideLeft,
                "SRA" => ReferenceChannel.SideRight,
                "SR" => ReferenceChannel.SideRight,
                "SBL" => ReferenceChannel.RearLeft,
                "SBR" => ReferenceChannel.RearRight,
                "TFL" => ReferenceChannel.TopFrontLeft,
                "TFR" => ReferenceChannel.TopFrontRight,
                "TRL" => ReferenceChannel.TopRearLeft,
                "TRR" => ReferenceChannel.TopRearRight,
                "FHL" => ReferenceChannel.TopFrontLeft,
                "FHR" => ReferenceChannel.TopFrontRight,
                "CH" => ReferenceChannel.TopFrontCenter,
                "TML" => ReferenceChannel.TopSideLeft,
                "TMR" => ReferenceChannel.TopSideRight,
                "RHL" => ReferenceChannel.TopRearLeft,
                "RHR" => ReferenceChannel.TopRearRight,
                "TS" => ReferenceChannel.GodsVoice,
                _ => ReferenceChannel.Unknown
            };
        }

        /// <summary>
        /// Get a nested JSON object if it exists, or create it when the source file omits it.
        /// </summary>
        static JsonFile GetOrCreateObject(JsonFile parent, string key) {
            foreach (var element in parent.Elements) {
                if (element.Key == key && element.Value is JsonFile value) {
                    return value;
                }
            }

            JsonFile result = new JsonFile();
            parent[key] = result;
            return result;
        }

        /// <summary>
        /// Find the nth occurrence of a channel with the specified reference type.
        /// </summary>
        /// <param name="reference">The reference channel type to find</param>
        /// <param name="occurrence">Which occurrence to find (0-based index)</param>
        /// <returns>The index in Channels array, or -1 if not found</returns>
        int FindNthChannel(ReferenceChannel reference, int occurrence) {
            int count = 0;
            for (int i = 0; i < Channels.Length; i++) {
                if (Channels[i].reference == reference) {
                    if (count == occurrence) {
                        return i;
                    }
                    count++;
                }
            }
            return -1;
        }

        /// <summary>
        /// The maximum allowed delay in milliseconds for MultEQ XT32.
        /// </summary>
        const double maxDelayMs = 20.0;
    }
}
