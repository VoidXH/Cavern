using System.IO;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for AU N-Band EQ software.
    /// </summary>
    public class AUNBandEQ : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 16;

        /// <inheritdoc/>
        public override double MinGain => -96;

        /// <inheritdoc/>
        public override double MaxGain => 24;

        /// <inheritdoc/>
        public override double GainPrecision => .1;

        /// <summary>
        /// IIR filter set for AU N-Band EQ software.
        /// </summary>
        public AUNBandEQ(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for AU N-Band EQ software.
        /// </summary>
        public AUNBandEQ(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export(false));
    }
}