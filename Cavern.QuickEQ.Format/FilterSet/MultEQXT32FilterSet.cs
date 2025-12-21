using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Equalization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Cavern.Format.FilterSet
{
    /// <summary>
    /// Equalizer filter set for MultEQ XT32.
    /// </summary>
    public class MultEQXT32FilterSet : EqualizerFilterSet
    {
        /// <summary>
        /// Extension of the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public override string FileExtension => "ady";

        /// <summary>
        /// Parsed JSON data from the source ADY file.
        /// </summary>
        public JsonFile? Data { get; private set; }

        /// <summary>
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Create a MultEQ XT32 configuration file for EQ export.
        /// </summary>
        public MultEQXT32FilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Load a MultEQ XT32 configuration file from disk.
        /// </summary>
        /// <param name="path">Path to the ADY file</param>
        public static MultEQXT32FilterSet FromFile(string path)
        {
            string fileContents = File.ReadAllText(path);

            JsonFile fileData = new JsonFile(fileContents);

            object[] detectedChannels = (object[])fileData["detectedChannels"];
            if (detectedChannels.Length == 0) {
                throw new CorruptionException("No channels found in ADY file");
            }

            // Map ADY channels to ReferenceChannels in the SAME ORDER
            ReferenceChannel[] channels = new ReferenceChannel[detectedChannels.Length];
            for (int i = 0; i < channels.Length; i++) {
                channels[i] = MapReference(((JsonFile)detectedChannels[i])["commandId"].ToString());
            }

            return new MultEQXT32FilterSet(channels, Listener.DefaultSampleRate) {
                Data = fileData
            };
        }

        /// <summary>
        /// Export the filter set to a MultEQ XT32 ADY file.
        /// </summary>
        /// <param name="path">Target file path</param>
        public override void Export(string path)
        {
            if (Data == null) {
                throw new InvalidSourceException();
            }

            Data["title"] = "Cavern QuickEQ";
            Data["dynamicEq"] = false;
            Data["dynamicVolume"] = false;
            Data["lfc"] = false;
            Data["enTargetCurveType"] = 1; // No HF rolloff I think

            double[] gains = GetGains(-12, 12);
            double[] delays = GetDelays(20);

            object[] detectedChannels = (object[])Data["detectedChannels"];

            for (int i = 0; i < detectedChannels.Length; i++) {
                EqualizerChannelData equalizerChannel = (EqualizerChannelData)Channels[i];
                JsonFile channelData = (JsonFile)detectedChannels[i];

                bool isSub = equalizerChannel.reference == ReferenceChannel.ScreenLFE;

                decimal level = (decimal)Math.Round(gains[i], 1);
                double distanceMeters = delays[i] * Source.SpeedOfSound / 1000.0;
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

            File.WriteAllText(path, Data.ToString());
        }

        /// <summary>
        /// Build the exported custom target curve points for a channel.
        /// </summary>
        string CreateCurve(Equalizer equalizer)
        {
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

        /// <summary>
        /// Get the delay of each channel in milliseconds, and confine them to the limits of the output format.
        /// </summary>
        double[] GetDelays(double maxDelay)
        {
            double[] result = new double[Channels.Length];
            double max = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                result[i] = Channels[i].delaySamples * 1000.0 / SampleRate;
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


        static ReferenceChannel MapReference(string commandId)
        {
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
    }
}