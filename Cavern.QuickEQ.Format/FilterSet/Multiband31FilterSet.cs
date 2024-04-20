using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Traditional 31-band graphic equalizer.
    /// </summary>
    public class Multiband31FilterSet : MultibandPEQFilterSet {
        /// <inheritdoc/>
        public Multiband31FilterSet(string path, int sampleRate) : base(path, sampleRate) => Prepare();

        /// <inheritdoc/>
        public Multiband31FilterSet(int channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <inheritdoc/>
        public Multiband31FilterSet(int channels, int sampleRate, bool roundedBands) :
            base(channels, sampleRate, 19.6862664, 3, 31, roundedBands) => Prepare();

        /// <inheritdoc/>
        public Multiband31FilterSet(ReferenceChannel[] channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <inheritdoc/>
        public Multiband31FilterSet(ReferenceChannel[] channels, int sampleRate, bool roundedBands) :
            base(channels, sampleRate, 19.6862664, 3, 31, roundedBands) => Prepare();

        /// <summary>
        /// Set up the rounding corrections to what most 31-band EQs use.
        /// </summary>
        void Prepare() {
            FreqOverrides = new[] {
                (31.0, 31.5),
                (39, 40),
                (62, 63),
                (79, 80),
                (99, 100),
                (120, 125),
                (310, 315),
                (790, 800),
                (1300, 1250),
                (3200, 3150),
                (13000, 12500)
            };
            LFEBands = 9;
        }
    }
}