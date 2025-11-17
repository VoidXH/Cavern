using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with infinite impulse response (biquad) filter sets for each channel.
    /// </summary>
    public class IIRFilterSet : FilterSet {
        /// <summary>
        /// All information needed for a channel filtered with IIR filters.
        /// </summary>
        public class IIRChannelData : ChannelData, IEquatable<IIRChannelData> {
            /// <summary>
            /// Applied filter set for the channel.
            /// </summary>
            public BiquadFilter[] filters;

            /// <summary>
            /// Gain offset for the channel.
            /// </summary>
            public double gain;

            /// <summary>
            /// Swap the sign for this channel.
            /// </summary>
            public bool switchPolarity;

            /// <summary>
            /// Check if the same correction is applied to the <paramref name="other"/> channel.
            /// </summary>
            public bool Equals(IIRChannelData other) => filters.Equals(other.filters) && gain == other.gain &&
                delaySamples == other.delaySamples && switchPolarity == other.switchPolarity;
        }

        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public virtual int Bands => 20;

        /// <summary>
        /// Limit the number of bands exported for the LFE channel.
        /// </summary>
        public virtual int LFEBands => Bands;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public virtual double MinGain => -20;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public virtual double MaxGain => 20;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public virtual double GainPrecision => .0001;

        /// <summary>
        /// Half the maximum Q factor allowed.
        /// </summary>
        public virtual double CenterQ => 10;

        /// <summary>
        /// What values to export per filter and in what order.
        /// </summary>
        public virtual FilterProperty[] Properties { get; } = new FilterProperty[] {
            FilterProperty.Frequency,
            FilterProperty.Gain,
            FilterProperty.QFactor
        };

        /// <summary>
        /// An optional text to add to the first line of every exported channel filter set.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target number of channels.
        /// </summary>
        public IIRFilterSet(int channels, int sampleRate) : base(sampleRate) => Initialize<IIRChannelData>(channels);

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target reference channels.
        /// </summary>
        public IIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) => Initialize<IIRChannelData>(channels);

        /// <summary>
        /// When specific operations like roundings must happen, override this function and post-process all filters to match the constraints.
        /// Gain limiting and rounding is excluded from here, as it currently happens in the PeakingEqualizer.
        /// </summary>
        public virtual void PostprocessFilter(PeakingEQ filter) { }

        /// <summary>
        /// If the filter set's band count is dependent on which channel is selected, use this function instead of <see cref="Bands"/>.
        /// </summary>
        public virtual int GetBands(ReferenceChannel channel) => channel == ReferenceChannel.ScreenLFE ? LFEBands : Bands;

        /// <summary>
        /// Convert the filter set to convolution impulse responses to be used with e.g. a <see cref="MultichannelConvolver"/>.
        /// </summary>
        public override MultichannelWaveform GetConvolutionFilter(int sampleRate, int convolutionLength) {
            float[][] result = new float[Channels.Length][];
            for (int i = 0; i < result.Length; i++) {
                result[i] = new float[convolutionLength];
                result[i][Channels[i].delaySamples] = 1;
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    BiquadFilter filter = (BiquadFilter)filters[j].Clone(sampleRate);
                    filter.Process(result[i]);
                }
            }
            return new MultichannelWaveform(result);
        }

        /// <summary>
        /// Setup a channel's filter set with only filters and no additional corrections or custom name.
        /// </summary>
        public void SetupChannel(int channel, BiquadFilter[] filters) => SetupChannel(channel, filters, 0, 0, false, null);

        /// <summary>
        /// Setup a channel's filter set with only filters and no additional corrections, but a custom name.
        /// </summary>
        public void SetupChannel(int channel, BiquadFilter[] filters, string name) => SetupChannel(channel, filters, 0, 0, false, name);

        /// <summary>
        /// Setup a channel's filter set with all corrections.
        /// </summary>
        public void SetupChannel(int channel, BiquadFilter[] filters, double gain, int delaySamples, bool switchPolarity, string name) {
            IIRChannelData channelRef = (IIRChannelData)Channels[channel];
            channelRef.filters = filters;
            channelRef.gain = gain;
            channelRef.delaySamples = delaySamples;
            channelRef.switchPolarity = switchPolarity;
            channelRef.name = name;
        }

        /// <summary>
        /// Setup a channel's filter set with only filters and no additional corrections or custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, BiquadFilter[] filters) => SetupChannel(channel, filters, 0, 0, false, null);

        /// <summary>
        /// Setup a channel's filter set with only filters and no additional corrections, but a custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, BiquadFilter[] filters, string name) =>
            SetupChannel(channel, filters, 0, 0, false, name);

        /// <summary>
        /// Setup a channel's filter set with all corrections.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, BiquadFilter[] filters,
            double gain, int delaySamples, bool switchPolarity, string name) {
            for (int i = 0; i < Channels.Length; i++) {
                if (Channels[i].reference == channel) {
                    SetupChannel(i, filters, gain, delaySamples, switchPolarity, name);
                    return;
                }
            }
        }

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

        /// <summary>
        /// Get the delay of each channel in milliseconds, and confine them to the limits of the output format.
        /// The longer end's relative differences will be kept, as the remaining channels are likely subwoofers.
        /// </summary>
        protected double[] GetDelays(double maxDelay) {
            double[] result = new double[Channels.Length];
            double max = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                result[i] = GetDelay(i);
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

        /// <summary>
        /// Get the gain of each channel in decibels, between the allowed limits of the output format.
        /// If the gains are not out of range, they will be returned as-is.
        /// </summary>
        protected double[] GetGains(double min, double max) {
            double[] result = new double[Channels.Length];
            double minFound = double.MaxValue, maxFound = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                result[i] = ((IIRChannelData)Channels[i]).gain;
                if (minFound > result[i]) {
                    minFound = result[i];
                }
                if (maxFound < result[i]) {
                    maxFound = result[i];
                }
            }
            if (minFound >= min && maxFound <= max) {
                return result;
            }

            double avg = QMath.Average(result);
            for (int i = 0; i < result.Length; i++) {
                result[i] = Math.Clamp(result[i] - avg, min, max);
            }

            return result;
        }
    }
}
