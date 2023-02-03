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
        protected struct ChannelData {
            /// <summary>
            /// Applied convolution filter to this channel.
            /// </summary>
            public float[] filter;

            /// <summary>
            /// Delay of this channel in samples.
            /// </summary>
            public int delaySamples;

            /// <summary>
            /// The reference channel describing this channel or <see cref="ReferenceChannel.Unknown"/> if not applicable.
            /// </summary>
            public ReferenceChannel reference;

            /// <summary>
            /// Custom label for this channel or null if not applicable.
            /// </summary>
            public string name;
        }

        /// <summary>
        /// Applied convolution filters for each channel in the configuration file.
        /// </summary>
        protected ChannelData[] Channels { get; private set; }

        /// <summary>
        /// Read a room correction with a FIR filter for each channel from a file.
        /// </summary>
        public FIRFilterSet(string path) : base(defaultSampleRate) {
            ReadFile(path, out ChannelData[] channels);
            Channels = channels;
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target number of channels.
        /// </summary>
        public FIRFilterSet(int channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < matrix.Length; i++) {
                Channels[i].reference = matrix[i];
            }
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target reference channels.
        /// </summary>
        public FIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i].reference = channels[i];
            }
        }

        /// <summary>
        /// Setup a channel's filter and related metadata.
        /// </summary>
        public void SetupChannel(int channel, float[] filter, int delaySamples = 0, string name = null) {
            Channels[channel].filter = filter;
            Channels[channel].delaySamples = delaySamples;
            Channels[channel].name = name;
        }

        /// <summary>
        /// Setup a channel's filter and related metadata.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, float[] filter, int delaySamples = 0, string name = null) {
            for (int i = 0; i < Channels.Length; ++i) {
                if (Channels[i].reference == channel) {
                    Channels[i].filter = filter;
                    Channels[i].delaySamples = delaySamples;
                    Channels[i].name = name;
                    return;
                }
            }
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels[channel].name ?? base.GetLabel(channel);

        /// <summary>
        /// When overridden, the filter set supports file import through this function.
        /// </summary>
        protected virtual void ReadFile(string path, out ChannelData[] channels) => throw new NotImplementedException();
    }
}