using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Traditional 31-band graphic equalizer.
    /// </summary>
    public class Multiband10FilterSet : MultibandPEQFilterSet {
        /// <inheritdoc/>
        public override int LFEBands => 3;

        /// <inheritdoc/>
        public Multiband10FilterSet(string path, int sampleRate) : base(path, sampleRate) { }

        /// <inheritdoc/>
        public Multiband10FilterSet(int channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <inheritdoc/>
        public Multiband10FilterSet(int channels, int sampleRate, bool roundedBands) :
            base(channels, sampleRate, 31.25, 1, 10, roundedBands) { }

        /// <inheritdoc/>
        public Multiband10FilterSet(ReferenceChannel[] channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <inheritdoc/>
        public Multiband10FilterSet(ReferenceChannel[] channels, int sampleRate, bool roundedBands) :
            base(channels, sampleRate, 31.25, 1, 10, roundedBands) { }
    }
}
