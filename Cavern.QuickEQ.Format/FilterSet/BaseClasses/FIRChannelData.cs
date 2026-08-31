using System;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// All information needed for a FIR-filtered channel.
    /// </summary>
    public class FIRChannelData : ChannelData, IEquatable<FIRChannelData> {
        /// <summary>
        /// When set, the exported WAV file changes its sample rate so there will be minimal IR noise above this frequency.
        /// </summary>
        public int? OverrideIRCutoff { get; set; }

        /// <summary>
        /// Applied convolution filter to this channel.
        /// </summary>
        public float[] filter;

        /// <inheritdoc/>
        public override object Clone() {
            FIRChannelData clone = (FIRChannelData)base.Clone();
            clone.filter = (float[])filter?.Clone();
            return clone;
        }

        /// <summary>
        /// Check if the same correction is applied to the <paramref name="other"/> channel.
        /// </summary>
        public bool Equals(FIRChannelData other) => Equals(filter, other?.filter) && delaySamples == other.delaySamples;
    }
}
