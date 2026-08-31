using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Multiband filter set for Trinnov hardware.
    /// </summary>
    public class TrinnovFilterSet : MultibandPEQFilterSet {
        /// <summary>
        /// Multiband filter set for Trinnov hardware.
        /// </summary>
        public TrinnovFilterSet(int channels) : base(channels, Listener.DefaultSampleRate, 10, 3, 34, true) => Prepare();

        /// <summary>
        /// Multiband filter set for Trinnov hardware.
        /// </summary>
        public TrinnovFilterSet(ReferenceChannel[] channels) : base(channels, Listener.DefaultSampleRate, 10, 3, 34, true) => Prepare();

        /// <summary>
        /// Set up generic bands for Trinnov display.
        /// </summary>
        void Prepare() => FreqOverrides = new[] {
            (13.0, 12.0),
            (32, 31),
            (40, 39),
            (63, 62),
            (80, 79),
            (100, 99),
            (130, 120),
            (320, 310),
            (510, 500),
            (640, 630),
            (810, 790),
            (2600, 2500),
            (4100, 4000),
            (5100, 5000),
            (6500, 6300),
            (8100, 7900)
        };
    }
}
