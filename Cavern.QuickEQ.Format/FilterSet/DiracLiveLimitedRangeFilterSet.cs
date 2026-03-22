using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live, cut off at 500 Hz.
    /// </summary>
    public class DiracLiveLimitedRangeFilterSet : DiracLiveFilterSet {
        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live, cut off at 500 Hz.
        /// </summary>
        public DiracLiveLimitedRangeFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live, cut off at 500 Hz.
        /// </summary>
        public DiracLiveLimitedRangeFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        protected override void ExportChannel(string path, Equalizer eq) =>
            eq.ExportToDirac(path, -eq[500], optionalHeader); // Fade to the continuous 0 at 500 Hz
    }
}
