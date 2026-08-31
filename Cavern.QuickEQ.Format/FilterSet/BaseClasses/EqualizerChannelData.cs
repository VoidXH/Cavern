using System;

using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
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

        /// <inheritdoc/>
        public override object Clone() {
            EqualizerChannelData clone = (EqualizerChannelData)base.Clone();
            clone.curve = (Equalizer)curve?.Clone();
            return clone;
        }

        /// <summary>
        /// Check if the same correction is applied to the <paramref name="other"/> channel.
        /// </summary>
        public bool Equals(EqualizerChannelData other) => Equals(curve, other?.curve) &&
            gain == other.gain && delaySamples == other.delaySamples && switchPolarity == other.switchPolarity;
    }
}
