using System.IO;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for WiiM devices.
    /// </summary>
    internal class WiiMFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 10;

        /// <inheritdoc/>
        public override double MinGain => -12;

        /// <inheritdoc/>
        public override double MaxGain => 12;

        /// <inheritdoc/>
        public override double GainPrecision => .1f;

        /// <summary>
        /// IIR filter set for WiiM devices.
        /// </summary>
        public WiiMFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for WiiM devices.
        /// </summary>
        public WiiMFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export(false));
    }
}