using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for StormAudio hardware.
    /// </summary>
    public class StormAudioFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of EQ bands per channel.
        /// </summary>
        public override int Bands => 12;

        /// <summary>
        /// Minimum gain of a single peaking EQ band.
        /// </summary>
        public override double MinGain => -18;

        /// <summary>
        /// Maximum gain of a single peaking EQ band.
        /// </summary>
        public override double MaxGain => 18;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for StormAudio with a given number of channels.
        /// </summary>
        public StormAudioFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            List<string> result = new List<string> {
                "Set up delays and levels by this file. Load \"Channel_....txt\" files as EQ."
            };
            for (int channel = 0, channels = Channels.Length; channel < channels; ++channel) {
                string chName = Channels[channel].name ??
                    (channel > 7 ? "CH" + (channel + 1) : channels < 7 ? labels51[channel] : labels71[channel]);
                result.Add(string.Empty);
                result.Add("Channel: " + chName);
                if (Channels[channel].delaySamples != 0) {
                    result.Add("Delay: " + GetDelay(Channels[channel].delaySamples).ToString("0.0 ms"));
                }
                result.Add("Level: " + Channels[channel].gain.ToString("0.0 dB"));
                if (Channels[channel].switchPolarity) {
                    result.Add("Switch polarity");
                }

                List<string> channelData = new List<string> {
                    "Equaliser: StormAudio",
                    string.Empty
                };
                BiquadFilter[] filters = Channels[channel].filters;
                for (int i = 0; i < filters.Length; i++) {
                    string freq;
                    if (filters[i].CenterFreq < 100) {
                        freq = filters[i].CenterFreq.ToString("0.00");
                    } else if (filters[i].CenterFreq < 1000) {
                        freq = filters[i].CenterFreq.ToString("0.0");
                    } else {
                        freq = filters[i].CenterFreq.ToString("0");
                    }
                    channelData.Add(string.Format("Filter {0,2}: ON  PK       Fc {1,7} Hz  Gain {2,6} dB  Q {3,6}",
                        i + 1, freq, filters[i].Gain.ToString("0.00", CultureInfo.InvariantCulture),
                        Math.Max(Math.Round(filters[i].Q * 4) / 4, .25).ToString("0.00", CultureInfo.InvariantCulture)));
                }
                for (int i = filters.Length; i < Bands;) {
                    channelData.Add($"Filter {++i}: OFF None");
                }
                channelData.Add("Filter 13: OFF None");
                channelData.Add("Filter 17: OFF None");
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {chName}.txt"), channelData);
            }
            File.WriteAllLines(path, result);
        }

        /// <summary>
        /// 5.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels51 = new string[] { "LF", "RF", "CF", "SUB", "LS", "RS" };

        /// <summary>
        /// 7.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels71 = new string[] { "LF", "RF", "CF", "SUB", "LB", "RB", "LS", "RS" };
    }
}