using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Generic IIR filter set for multiple hardware.
    /// </summary>
    public class GenericFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 20;

        /// <summary>
        /// Minimum gain of a single peaking EQ band.
        /// </summary>
        public override double MinGain => -20;

        /// <summary>
        /// Maximum gain of a single peaking EQ band.
        /// </summary>
        public override double MaxGain => 20;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .0001;

        /// <summary>
        /// Generic IIR filter set for multiple hardware.
        /// </summary>
        public GenericFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            for (int i = 0, c = Channels.Length; i < c; i++) {
                List<string> channelData = new List<string>();
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
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt"), channelData);
            }
        }
    }
}