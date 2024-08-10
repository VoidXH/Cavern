using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;

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
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            for (int i = 0, c = Channels.Length; i < c; i++) {
                List<string> channelData = new List<string>();
                if (Header != null) {
                    channelData.Add(Header);
                }

                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    string freq;
                    if (filters[j].CenterFreq < 100) {
                        freq = filters[j].CenterFreq.ToString("0.00", Culture);
                    } else if (filters[j].CenterFreq < 1000) {
                        freq = filters[j].CenterFreq.ToString("0.0", Culture);
                    } else {
                        freq = filters[j].CenterFreq.ToString("0", Culture);
                    }
                    channelData.Add(string.Format("Filter {0,2}: ON  PK       Fc {1,7} Hz  Gain {2,6} dB  Q {3,6}",
                        j + 1, freq, filters[j].Gain.ToString("0.00", Culture),
                        Math.Max(Math.Round(filters[j].Q * 4) / 4, .25).ToString("0.00", Culture)));
                }
                for (int j = filters.Length; j < Bands;) {
                    channelData.Add($"Filter {++j}: OFF None");
                }
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt"), channelData);
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
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                result.AppendLine(string.Empty);
                string chName = GetLabel(i);
                result.AppendLine(chName);
                result.AppendLine(new string('=', chName.Length));
                RootFileExtension(i, result);
                if (channelRef.delaySamples != 0) {
                    result.AppendLine("Delay: " + GetDelay(i).ToString("0.00 ms", Culture));
                }

                BiquadFilter[] bands = channelRef.filters;
                if (gainOnly) {
                    for (int j = 0; j < bands.Length; j++) {
                        result.AppendLine($"{bands[j].CenterFreq.ToString("0", Culture)} Hz:\t{bands[j].Gain.ToString("0.00", Culture)} dB");
                    }
                } else {
                    for (int j = 0; j < bands.Length;) {
                        BiquadFilter filter = bands[j];
                        result.AppendLine($"Filter {++j}:").
                            AppendLine($"- Frequency: {filter.CenterFreq.ToString("0", Culture)} Hz").
                            AppendLine("- Q factor: " + filter.Q.ToString("0.00", Culture)).
                            AppendLine($"- Gain: {filter.Gain.ToString("0.00", Culture)} dB");
                    }
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        protected override void RootFileExtension(int channel, StringBuilder result) {
            IIRChannelData channelRef = (IIRChannelData)Channels[channel];
            if (channelRef.gain != 0) {
                result.AppendLine("Gain: " + channelRef.gain.ToString("0.00 dB"));
            }
            if (channelRef.switchPolarity) {
                result.AppendLine("Switch polarity");
            }
        }
    }
}