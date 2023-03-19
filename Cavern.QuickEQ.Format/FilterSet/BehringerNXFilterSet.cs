using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Behringer NX-series stereo amplifiers.
    /// </summary>
    public class BehringerNXFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 8;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MinGain => -15;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 6;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for Behringer NX-series stereo amplifiers with a given number of channels.
        /// </summary>
        public BehringerNXFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Behringer NX-series stereo amplifiers with a given number of channels.
        /// </summary>
        public BehringerNXFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "nxp");

            List<string> channelData = new List<string>();
            for (int i = 0, c = Channels.Length; i < c; ++i) {
                if ((i & 1) == 0) {
                    channelData.Clear();
                    channelData.AddRange(header);
                }

                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                string prefix = $"/channel/{i % 2 + 1}/";
                BiquadFilter[] filters = channelRef.filters;
                for (int j = 0; j < filters.Length; j++) {
                    string freq;
                    if (filters[j].CenterFreq < 1000) {
                        freq = filters[j].CenterFreq.ToString("0.0", CultureInfo.InvariantCulture);
                    } else {
                        int freqSource = (int)(filters[j].CenterFreq + .5);
                        freq = $"{freqSource / 1000}k{freqSource % 1000 / 10}";
                    }
                    channelData.Add(string.Format("{0}peq/{1} sfff PEQ {2} {3,5} {4}", prefix, j + 1,
                        freq, FormatWithSign(filters[j].Gain, "0.0"),
                        Math.Min(filters[j].Q, 10).ToString("0.00", CultureInfo.InvariantCulture)));
                }
                channelData.AddRange(new[] {
                    prefix + "xover/hp sf OFF 100.0",
                    prefix + "xover/lp sf OFF 10k00",
                    $"{prefix}xover/gain f  {FormatWithSign(Math.Max(Math.Round(channelRef.gain * 5) / 5, -12), "0.0")}",
                    prefix + "deq/1/comp fff  +0.0 -60.0 5.0",
                    prefix + "deq/1/time ff 20.0 400.0",
                    prefix + "deq/1/filt sff OFF 200.0 1.00",
                    prefix + "deq/2/comp fff  +0.0 -60.0 5.0",
                    prefix + "deq/2/time ff 20.0 400.0",
                    prefix + "deq/2/filt sff OFF 2k00 1.00",
                    $"{prefix}delay fi   {GetDelay(i).ToString("0.00", CultureInfo.InvariantCulture)} 0",
                    prefix + "limiter fff 85.0 100.0  50.0"
                });

                if ((i & 1) != 0) {
                    channelData.Add("END_OSC_DATA");
                    File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i - 1)}-{GetLabel(i)}.nxp"), channelData);
                }
            }
        }

        static string FormatWithSign(double number, string format) {
            string parse = number.ToString(format, CultureInfo.InvariantCulture);
            if (number >= 0) {
                return '+' + parse;
            }
            return parse;
        }

        /// <summary>
        /// Beginning of each .nxp file.
        /// </summary>
        static readonly string[] header = { "QuickEQ", "BEGIN_OSC_DATA", "/ampmode s DUAL" };
    }
}