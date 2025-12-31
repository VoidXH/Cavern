using System;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with infinite impulse response (biquad) filter sets for each channel.
    /// </summary>
    public partial class IIRFilterSet : FilterSet {
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
        /// Round the Qs to this precision.
        /// </summary>
        public virtual double QPrecision => .0001;

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
        public virtual void PostprocessFilter(PeakingEQ filter) => filter.Q = SnapQ(filter.Q);

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

        /// <inheritdoc/>
        public override double GetPeak() {
            double peak = double.MinValue;
            for (int i = 0; i < Channels.Length; i++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                if (peak < channelRef.gain) {
                    peak = channelRef.gain;
                }
            }
            return peak;
        }

        /// <summary>
        /// Sets the requested <paramref name="q"/> to a value that's permitted by the device.
        /// </summary>
        protected double SnapQ(double q) => Math.Round(q / QPrecision) * QPrecision;
    }
}
