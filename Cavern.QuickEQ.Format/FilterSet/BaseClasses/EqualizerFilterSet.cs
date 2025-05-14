using System;
using System.IO;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with an EQ curve for each channel. This base class exports standard EQ curves compatible with most
    /// applications using frequency/gain pairs in TXT files.
    /// </summary>
    public class EqualizerFilterSet : FilterSet {
        /// <summary>
        /// Additional text to be added to the first line of each output as comment.
        /// </summary>
        public string optionalHeader;

        /// <summary>
        /// Required data for each exported channel.
        /// </summary>
        public class EqualizerChannelData : ChannelData, IEquatable<EqualizerChannelData> {
            /// <summary>
            /// Applied equalization filter for the channel, using which is resulting in the expected target response.
            /// </summary>
            public Equalizer curve;

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
            public bool Equals(EqualizerChannelData other) => curve.Equals(other.curve) &&
                gain == other.gain && delaySamples == other.delaySamples && switchPolarity == other.switchPolarity;
        }

        /// <summary>
        /// Construct a room correction with EQ curves for each channel for a room with the target number of channels.
        /// </summary>
        public EqualizerFilterSet(int channels, int sampleRate) : base(sampleRate) => Initialize<EqualizerChannelData>(channels);

        /// <summary>
        /// Construct a room correction with EQ curves for each channel for a room with the target reference channels.
        /// </summary>
        public EqualizerFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) =>
            Initialize<EqualizerChannelData>(channels);

        /// <summary>
        /// Construct an <see cref="EqualizerFilterSet"/> without the data of <see cref="Channels"/> pre-created.
        /// This is useful when you also derive <see cref="EqualizerChannelData"/> and call <see cref="FilterSet.Initialize{T}(int)"/>.
        /// </summary>
        protected EqualizerFilterSet(int sampleRate) : base(sampleRate) { }

        /// <summary>
        /// Convert the filter set to convolution impulse responses to be used with e.g. a <see cref="MultichannelConvolver"/>.
        /// </summary>
        public override MultichannelWaveform GetConvolutionFilter(int sampleRate, int convolutionLength) {
            float[][] result = new float[Channels.Length][];
            for (int i = 0; i < result.Length; i++) {
                Equalizer curve = ((EqualizerChannelData)Channels[i]).curve;
                if (curve != null) {
                    result[i] = curve.GetConvolution(sampleRate, convolutionLength);
                } else {
                    result[i] = new float[convolutionLength];
                    result[i][0] = 1; // Dirac-delta, can be delayed
                }
                WaveformUtils.Delay(result[i], Channels[i].delaySamples);
            }
            return new MultichannelWaveform(result);
        }

        /// <summary>
        /// Setup a channel's curve with no additional gain/delay or custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve) => SetupChannel(channel, curve, 0, 0, false, null);

        /// <summary>
        /// Setup a channel's curve with additional gain/delay, but no custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, double gain, int delaySamples) =>
            SetupChannel(channel, curve, gain, delaySamples, false, null);

        /// <summary>
        /// Setup a channel's curve with a custom name, but no additional gain/delay.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, string name) => SetupChannel(channel, curve, 0, 0, false, name);

        /// <summary>
        /// Setup a channel's curve with additional gain/delay, and a custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, double gain, int delaySamples, bool switchPolarity, string name) {
            EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
            channelRef.curve = curve;
            channelRef.gain = gain;
            channelRef.delaySamples = delaySamples;
            channelRef.switchPolarity = switchPolarity;
            channelRef.name = name;
        }

        /// <summary>
        /// Setup a channel's curve with no additional gain/delay or custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, Equalizer curve) => SetupChannel(channel, curve, 0, 0, false,null);

        /// <summary>
        /// Setup a channel's curve with additional gain/delay, but no custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, Equalizer curve, double gain, int delaySamples) =>
            SetupChannel(channel, curve, gain, delaySamples, false, null);

        /// <summary>
        /// Setup a channel's curve with a custom name, but no additional gain/delay.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, Equalizer curve, string name) => SetupChannel(channel, curve, 0, 0, false, name);

        /// <summary>
        /// Setup a channel's curve with additional gain/delay, and a custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, Equalizer curve, double gain, int delaySamples,
            bool switchPolarity, string name) {
            for (int i = 0; i < Channels.Length; i++) {
                if (Channels[i].reference == channel) {
                    EqualizerChannelData channelRef = (EqualizerChannelData)Channels[i];
                    channelRef.curve = curve;
                    channelRef.gain = gain;
                    channelRef.delaySamples = delaySamples;
                    channelRef.switchPolarity = switchPolarity;
                    channelRef.name = name;
                    return;
                }
            }
        }

        /// <summary>
        /// Save the results to EQ curve files for each channel.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);
            bool csv = path.EndsWith(".csv");
            for (int i = 0; i < Channels.Length; i++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[i];
                string chName = string.IsNullOrEmpty(Channels[i].name)
                    ? Channels[i].reference.GetShortName()
                    : Channels[i].name;
                string fileName = Path.Combine(folder, $"{fileNameBase} {chName}");
                if (csv) {
                    channelRef.curve.ExportCSV(fileName + ".csv", 0);
                } else {
                    channelRef.curve.Export(fileName + ".txt", 0, optionalHeader, Culture);
                }
            }
        }

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        protected override bool RootFileExtension(int channel, StringBuilder result) {
            EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
            bool written = false;
            if (channelRef.gain != 0) {
                result.AppendLine("Level: " + channelRef.gain.ToString("0.0 dB"));
                written = true;
            }
            return written;
        }
    }
}