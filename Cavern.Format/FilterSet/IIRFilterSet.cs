using System;

using Cavern.Filters;
using Cavern.Remapping;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with infinite impulse response (biquad) filter sets for each channel.
    /// </summary>
    public abstract class IIRFilterSet : FilterSet {
        /// <summary>
        /// All information needed for a channel.
        /// </summary>
        protected struct ChannelData {
            /// <summary>
            /// Applied filter set for the channel.
            /// </summary>
            public BiquadFilter[] filters;

            /// <summary>
            /// Gain offset for the channel.
            /// </summary>
            public double gain;

            /// <summary>
            /// Delay of this channel in samples.
            /// </summary>
            public int delaySamples;

            /// <summary>
            /// Swap the sign for this channel.
            /// </summary>
            public bool switchPolarity;

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
        /// Maximum number of EQ bands per channel.
        /// </summary>
        public abstract int Bands { get; }

        /// <summary>
        /// Minimum gain of a single peaking EQ band.
        /// </summary>
        public abstract double MinGain { get; }

        /// <summary>
        /// Maximum gain of a single peaking EQ band.
        /// </summary>
        public abstract double MaxGain { get; }

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public virtual double GainPrecision => .01;

        /// <summary>
        /// Applied filter sets for each channel in the configuration file.
        /// </summary>
        protected ChannelData[] Channels { get; private set; }

        /// <summary>
        /// Read a room correction with IIR filter sets for each channel from a file.
        /// </summary>
        public IIRFilterSet(string path) : base(defaultSampleRate) {
            ReadFile(path, out ChannelData[] channels);
            Channels = channels;
        }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target number of channels.
        /// </summary>
        public IIRFilterSet(int channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < matrix.Length; i++) {
                Channels[i].reference = matrix[i];
            }
        }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target reference channels.
        /// </summary>
        public IIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i].reference = channels[i];
            }
        }

        /// <summary>
        /// Setup a channel's filter set and related metadata.
        /// </summary>
        public void SetupChannel(int channel, BiquadFilter[] filters, double gain = 0, int delaySamples = 0,
            bool switchPolarity = false, string name = null) {
            Channels[channel].filters = filters;
            Channels[channel].gain = gain;
            Channels[channel].delaySamples = delaySamples;
            Channels[channel].switchPolarity = switchPolarity;
            Channels[channel].name = name;
        }

        /// <summary>
        /// Setup a channel's filter set and related metadata.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, BiquadFilter[] filters,
            double gain = 0, int delaySamples = 0, bool switchPolarity = false, string name = null) {
            for (int i = 0; i < Channels.Length; i++) {
                if (Channels[i].reference == channel) {
                    Channels[i].filters = filters;
                    Channels[i].gain = gain;
                    Channels[i].delaySamples = delaySamples;
                    Channels[i].switchPolarity = switchPolarity;
                    Channels[i].name = name;
                }
            }
        }

        /// <summary>
        /// When overridden, the filter set supports file import through this function.
        /// </summary>
        protected virtual void ReadFile(string path, out ChannelData[] channels) => throw new NotImplementedException();
    }
}