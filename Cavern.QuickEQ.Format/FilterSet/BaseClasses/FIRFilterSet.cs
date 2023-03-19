using System;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with a finite impulse response (convolution) filter for each channel.
    /// </summary>
    public abstract class FIRFilterSet : FilterSet {
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
        protected FIRFilterSet(int channels, int sampleRate) : base(sampleRate) {
            Channels = new FIRChannelData[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < matrix.Length; i++) {
                Channels[i] = new FIRChannelData {
                    reference = matrix[i]
                };
            }
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target reference channels.
        /// </summary>
        protected FIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) {
            Channels = new FIRChannelData[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i] = new FIRChannelData {
                    reference = channels[i]
                };
            }
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
                    ((FIRChannelData)Channels[i]).filter = filter;
                    Channels[i].delaySamples = delaySamples;
                    Channels[i].name = name;
                    return;
                }
            }
        }
    }
}