using System;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
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

        /// <inheritdoc/>
        public override object Clone() {
            IIRChannelData clone = (IIRChannelData)base.Clone();
            clone.filters = filters?.DeepCopy1D();
            return clone;
        }

        /// <summary>
        /// Check if the same correction is applied to the <paramref name="other"/> channel.
        /// </summary>
        public bool Equals(IIRChannelData other) => Equals(filters, other?.filters) && gain == other.gain &&
            delaySamples == other.delaySamples && switchPolarity == other.switchPolarity;
    }
}
