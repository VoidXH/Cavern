using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Filter set limited to 4/3 octave band choices for some versions of YPAO.
    /// </summary>
    public class YPAOLiteFilterSet : MultibandPEQFilterSet {
        /// <inheritdoc/>
        public override double MinGain => -6;

        /// <summary>
        /// Filter set limited to 4/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOLiteFilterSet(int channels, int sampleRate) : base(channels, sampleRate, 62.5, .75, 7) => LFEBands = 2;

        /// <summary>
        /// Filter set limited to 4/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOLiteFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate, 62.5, .75, 7) => LFEBands = 2;
    }
}