﻿using System;
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
        /// IIR filter set for StormAudio processors with a given number of channels.
        /// </summary>
        public StormAudioFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            for (int i = 0, c = Channels.Length; i < c; i++) {
                List<string> channelData = new List<string> {
                    "Equaliser: StormAudio",
                    string.Empty
                };
                BiquadFilter[] filters = Channels[i].filters;
                for (int j = 0; j < filters.Length; j++) {
                    string freq;
                    if (filters[j].CenterFreq < 100) {
                        freq = filters[j].CenterFreq.ToString("0.00");
                    } else if (filters[j].CenterFreq < 1000) {
                        freq = filters[j].CenterFreq.ToString("0.0");
                    } else {
                        freq = filters[j].CenterFreq.ToString("0");
                    }
                    channelData.Add(string.Format("Filter {0,2}: ON  PK       Fc {1,7} Hz  Gain {2,6} dB  Q {3,6}",
                        j + 1, freq, filters[j].Gain.ToString("0.00", CultureInfo.InvariantCulture),
                        Math.Max(Math.Round(filters[j].Q * 4) / 4, .25).ToString("0.00", CultureInfo.InvariantCulture)));
                }
                for (int j = filters.Length; j < Bands;) {
                    channelData.Add($"Filter {++j}: OFF None");
                }
                channelData.Add("Filter 13: OFF None");
                channelData.Add("Filter 17: OFF None");
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt"), channelData);
            }
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels[channel].name ?? (channel > 7 ? "CH" + (channel + 1) :
            Channels.Length < 7 ? labels51[channel] : labels71[channel]);

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