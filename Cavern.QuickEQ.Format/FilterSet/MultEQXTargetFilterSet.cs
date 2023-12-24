using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Target curve collection for MultEQ-X to be able to use its own EQ.
    /// </summary>
    public class MultEQXTargetFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Target curve collection for MultEQ-X to be able to use its own EQ.
        /// </summary>
        public MultEQXTargetFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Target curve collection for MultEQ-X to be able to use its own EQ.
        /// </summary>
        public MultEQXTargetFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }
    }
}