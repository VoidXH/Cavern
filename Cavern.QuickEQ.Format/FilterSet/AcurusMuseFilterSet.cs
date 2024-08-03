using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Acurus Muse processors.
    /// </summary>
    public class AcurusMuseFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 5;

        /// <inheritdoc/>
        public override double MinGain => -12;

        /// <inheritdoc/>
        public override double MaxGain => 6;

        /// <inheritdoc/>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for Acurus Muse processors.
        /// </summary>
        public AcurusMuseFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Acurus Muse processors.
        /// </summary>
        public AcurusMuseFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }
    }
}