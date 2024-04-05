using System;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with a finite impulse response (convolution) filter for each channel.
    /// </summary>
    public class FIRFilterSet : FilterSet {
        /// <inheritdoc/>
        public override string FileExtension => "wav";

        /// <summary>
        /// All information needed for a channel.
        /// </summary>
        protected class FIRChannelData : ChannelData, IEquatable<FIRChannelData> {
            /// <summary>
            /// Applied convolution filter to this channel.
            /// </summary>
            public float[] filter;

            /// <summary>
            /// Check if the same correction is applied to the <paramref name="other"/> channel.
            /// </summary>
            public bool Equals(FIRChannelData other) => filter.Equals(other.filter) && delaySamples == other.delaySamples;
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target number of channels.
        /// </summary>
        public FIRFilterSet(int channels, int sampleRate) : base(sampleRate) => Initialize<FIRChannelData>(channels);

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target reference channels.
        /// </summary>
        public FIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) => Initialize<FIRChannelData>(channels);

        /// <summary>
        /// Convert the filter set to convolution impulse responses to be used with e.g. a <see cref="MultichannelConvolver"/>.
        /// </summary>
        public override MultichannelWaveform GetConvolutionFilter(int sampleRate, int convolutionLength) {
            float[][] result = new float[Channels.Length][];
            for (int i = 0; i < result.Length; i++) {
                result[i] = new float[convolutionLength];
                float[] source = ((FIRChannelData)Channels[i]).filter;
                if (SampleRate != sampleRate) {
                    source = Resample.Adaptive(source, (int)((long)source.Length * SampleRate / sampleRate), QualityModes.Perfect);
                }
                Array.Copy(source, result[i], Math.Min(source.Length, convolutionLength));
                WaveformUtils.Delay(result[i], Channels[i].delaySamples);
            }
            return new MultichannelWaveform(result);
        }

        /// <summary>
        /// Setup a channel's filter with no delay or custom name.
        /// </summary>
        public void SetupChannel(int channel, float[] filter) => SetupChannel(channel, filter, 0, null);

        /// <summary>
        /// Setup a channel's filter with additional delay, but no custom name.
        /// </summary>
        public void SetupChannel(int channel, float[] filter, int delaySamples) => SetupChannel(channel, filter, delaySamples, null);

        /// <summary>
        /// Setup a channel's filter with a custom name, but no additional delay.
        /// </summary>
        public void SetupChannel(int channel, float[] filter, string name) => SetupChannel(channel, filter, 0, name);

        /// <summary>
        /// Setup a channel's filter with additional delay and custom name.
        /// </summary>
        public void SetupChannel(int channel, float[] filter, int delaySamples, string name) {
            ((FIRChannelData)Channels[channel]).filter = filter;
            Channels[channel].delaySamples = delaySamples;
            Channels[channel].name = name;
        }

        /// <summary>
        /// Setup a channel's filter with no additional delay or custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, float[] filter) => SetupChannel(channel, filter, 0, null);

        /// <summary>
        /// Setup a channel's filter with additional delay, but no custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, float[] filter, int delaySamples) =>
            SetupChannel(channel, filter, delaySamples, null);

        /// <summary>
        /// Setup a channel's filter with a custom name, but no additional delay.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, float[] filter, string name) => SetupChannel(channel, filter, 0, name);

        /// <summary>
        /// Setup a channel's filter with additional delay and custom name.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, float[] filter, int delaySamples, string name) {
            for (int i = 0; i < Channels.Length; ++i) {
                if (Channels[i].reference == channel) {
                    SetupChannel(i, filter, delaySamples, name);
                    return;
                }
            }
        }

        /// <inheritdoc/>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            for (int i = 0; i < Channels.Length; i++) {
                FIRChannelData channelRef = (FIRChannelData)Channels[i];
                string label = channelRef.name ?? EqualizerAPOUtils.GetChannelLabel(i, Channels.Length),
                    filterPath = Path.Combine(folder, $"{fileNameBase} {label}.wav");
                RIFFWaveWriter.Write(filterPath, channelRef.filter, 1, SampleRate, BitDepth.Float32);
            }
        }
    }
}