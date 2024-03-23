using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Filter curve collection for MultEQ-X.
    /// </summary>
    public class MultEQXTargetFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Filter curve collection for MultEQ-X.
        /// </summary>
        public MultEQXTargetFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Filter curve collection for MultEQ-X.
        /// </summary>
        public MultEQXTargetFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }
    }
}