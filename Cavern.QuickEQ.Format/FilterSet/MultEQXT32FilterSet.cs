using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Equalization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a MultEQ XT32 ADY file.
        /// </summary>
        /// <param name="path">Target file path</param>
        public override void Export(string path) {
            string fileContents = File.ReadAllText(path);
            JsonFile data = new JsonFile(fileContents);

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

                bool isSub = equalizerChannel.reference == ReferenceChannel.ScreenLFE;

                decimal level = (decimal)Math.Round(gains[eqIndex], 1);
                double distanceMeters = Math.Min(GetDelay(eqIndex), maxDelayMs) * Source.SpeedOfSound / 1000.0;
                decimal distance = (decimal)Math.Round(distanceMeters, 2);

                channelData["delayAdjustment"] = "0.0";
                channelData["trimAdjustment"] = "0.0";
                channelData["customDistance"] = distance;
                channelData["customLevel"] = level.ToString("0.0", CultureInfo.InvariantCulture);
                channelData["frequencyRangeRolloff"] = isSub ? 250 : 20000;
                channelData["customTargetCurvePoints"] = "[" + CreateCurve(equalizerChannel.curve) + "]";

                JsonFile channelReport = (JsonFile)channelData["channelReport"];
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

            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);

            File.WriteAllText(Path.Combine(folder, $"{fileNameBase} modified.ady"), data.ToString());
        }

        /// <summary>
        /// Build the exported custom target curve points for a channel.
        /// </summary>
        string CreateCurve(Equalizer equalizer) {
            if (equalizer == null || equalizer.Bands.Count == 0) {
                return "";
            }

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < equalizer.Bands.Count; i++) {
                if (i > 0) {
                    result.Append(',');
                }

                result.Append('{');
                result.Append(equalizer.Bands[i].Frequency.ToString(CultureInfo.InvariantCulture));
                result.Append(',');
                result.Append(equalizer.Bands[i].Gain.ToString(CultureInfo.InvariantCulture));
                result.Append('}');
            }

            return result.ToString();
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
