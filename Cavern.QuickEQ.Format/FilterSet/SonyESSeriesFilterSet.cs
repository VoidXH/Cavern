using Cavern.Channels;
using Cavern.Format.FilterSet.BaseClasses;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Banded filter set for Sony ES-series receivers.
    /// </summary>
    public class SonyESSeriesFilterSet : LimitedEqualizerFilterSet {
        /// <inheritdoc/>
        protected override float[] Frequencies => frequencies;

        /// <inheritdoc/>
        protected override float[] LFEFrequencies => lfeFrequencies;

        /// <inheritdoc/>
        protected override float Smoothing => 1;

        /// <summary>
        /// Banded filter set for Sony ES-series receivers.
        /// </summary>
        public SonyESSeriesFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Banded filter set for Sony ES-series receivers.
        /// </summary>
        public SonyESSeriesFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// All frequency bands that need to be set.
        /// </summary>
        static readonly float[] frequencies = { 47, 230, 470, 840, 1300, 2300, 3800, 5800, 9000, 14000 };

        /// <summary>
        /// All LFE frequency bands that need to be set.
        /// </summary>
        static readonly float[] lfeFrequencies = { 40, 60, 80, 90, 100, 120 };
    }
}