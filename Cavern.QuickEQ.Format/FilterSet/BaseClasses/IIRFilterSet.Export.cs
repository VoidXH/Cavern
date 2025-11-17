using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    // Export handlers of generic PEQ files
    partial class IIRFilterSet {
        /// <summary>
        /// Export the filter set to a target file. This is the standard IIR filter set format.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);
            CreateRootFile(path, FileExtension);

            for (int i = 0, c = Channels.Length; i < c; i++) {
                List<string> channelData = new List<string>();
                if (Header != null) {
                    channelData.Add(Header);
                }

                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    string freq = RangeDependentDecimals(filters[j].CenterFreq);
                    channelData.Add(string.Format("Filter {0,2}: ON  PK       Fc {1,7} Hz  Gain {2,6} dB  Q {3,6}",
                        j + 1, freq, QMath.ToStringLimitDecimals(filters[j].Gain, 2),
                        QMath.ToStringLimitDecimals(Math.Max(Math.Round(filters[j].Q * 4) / 4, .25), 2)));
                }
                for (int j = filters.Length; j < Bands;) {
                    channelData.Add($"Filter {++j}: OFF None");
                }
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.{FileExtension}"), channelData);
            }
        }

        /// <summary>
        /// Export the filter set for manual per-band import, formatted as a single text to be displayed.
        /// </summary>
        public virtual string Export() => Export(false);

        /// <summary>
        /// Export the filter set for manual per-band import, formatted as a single text to be displayed.
        /// </summary>
        /// <param name="gainOnly">Don't export the Q factor - this is useful when they are all the same,
        /// like for <see cref="Multiband31FilterSet"/></param>
        protected virtual string Export(bool gainOnly) {
            StringBuilder result = new StringBuilder("Set up the channels according to this configuration.").AppendLine();
            for (int i = 0; i < Channels.Length; i++) {
                RootFileChannelHeader(i, result, true);
                BiquadFilter[] bands = ((IIRChannelData)Channels[i]).filters;
                if (gainOnly) {
                    for (int j = 0; j < bands.Length; j++) {
                        string gain = QMath.ToStringLimitDecimals(bands[j].Gain, 2);
                        result.AppendLine($"{RangeDependentDecimals(bands[j].CenterFreq)} Hz:\t{gain} dB");
                    }
                } else {
                    for (int j = 0; j < bands.Length;) {
                        BiquadFilter filter = bands[j];
                        result.AppendLine($"Filter {++j}:");
                        for (int prop = 0; prop < Properties.Length; prop++) {
                            switch (Properties[prop]) {
                                case FilterProperty.Gain:
                                    result.AppendLine($"- Gain: {QMath.ToStringLimitDecimals(filter.Gain, 2)} dB");
                                    break;
                                case FilterProperty.Frequency:
                                    result.AppendLine($"- Frequency: {RangeDependentDecimals(filter.CenterFreq)} Hz");
                                    break;
                                case FilterProperty.QFactor:
                                    result.AppendLine("- Q factor: " + QMath.ToStringLimitDecimals(filter.Q, 2));
                                    break;
                            }
                        }
                    }
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        protected override bool RootFileExtension(int channel, StringBuilder result) {
            IIRChannelData channelRef = (IIRChannelData)Channels[channel];
            bool written = false;
            if (channelRef.gain != 0) {
                result.AppendLine($"Gain: {QMath.ToStringLimitDecimals(channelRef.gain, 2)} dB");
                written = true;
            }
            if (channelRef.switchPolarity) {
                result.AppendLine("Switch polarity");
                written = true;
            }
            return written;
        }
    }
}
