using System;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Basic information needed for a channel.
    /// </summary>
    public abstract class ChannelData : ICloneable {
        /// <summary>
        /// The reference channel describing this channel or <see cref="ReferenceChannel.Unknown"/> if not applicable.
        /// </summary>
        public ReferenceChannel reference;

        /// <summary>
        /// Custom label for this channel or null if not applicable.
        /// </summary>
        public string name;

        /// <summary>
        /// Delay of this channel in samples.
        /// </summary>
        public int delaySamples;

        /// <inheritdoc/>
        public virtual object Clone() => MemberwiseClone();
    }
}
