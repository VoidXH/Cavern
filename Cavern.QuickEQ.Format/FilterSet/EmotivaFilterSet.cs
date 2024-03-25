using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Emotiva hardware.
    /// </summary>
    public class EmotivaFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 12;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MinGain => -12;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 6;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for Emotiva processors with a given number of channels.
        /// </summary>
        public EmotivaFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Emotiva processors with a given number of channels.
        /// </summary>
        public EmotivaFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "emo");

            for (int i = 0, c = Channels.Length; i < c; ++i) {
                List<string> channelData = new List<string> {
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
                    "<equalization>",
                    $"    <speaker location=\"{GetLabel(i)}\" headroom=\"6\">"
                };
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    string qFactor = Math.Min(Math.Round(filters[j].Q * 5) / 5, 10).ToString("0.00", CultureInfo.InvariantCulture);
                    channelData.Add($"        <filter number=\"{j + 1}\">");
                    channelData.Add($"            <frequency>{filters[j].CenterFreq:0}</frequency>");
                    channelData.Add($"            <level>{filters[j].Gain.ToString("0.0", CultureInfo.InvariantCulture)}</level>");
                    channelData.Add($"            <Q>{qFactor}</Q>");
                    channelData.Add("        </filter>");
                }
                channelData.Add("    </speaker>");
                channelData.Add("</equalization>");
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.emo"), channelData);
            }
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels[channel].reference switch {
            ReferenceChannel.FrontLeft => "leftFront",
            ReferenceChannel.FrontRight => "rightFront",
            ReferenceChannel.FrontCenter => "center",
            ReferenceChannel.ScreenLFE => "centerSubwoofer",
            ReferenceChannel.RearLeft => "leftBack",
            ReferenceChannel.RearRight => "rightBack",
            ReferenceChannel.SideLeft => "leftSurround",
            ReferenceChannel.SideRight => "rightSurround",
            ReferenceChannel.TopFrontLeft => "leftFrontHeight",
            ReferenceChannel.TopFrontRight => "rightFrontHeight",
            ReferenceChannel.TopSideLeft => "leftMiddleHeight",
            ReferenceChannel.TopSideRight => "rightMiddleHeight",
            ReferenceChannel.TopRearLeft => "leftRearHeight",
            ReferenceChannel.TopRearRight => "rightRearHeight",
            _ => base.GetLabel(channel)
        };
    }
}