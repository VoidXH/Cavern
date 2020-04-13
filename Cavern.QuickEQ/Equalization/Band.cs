namespace Cavern.QuickEQ.Equalization {
    /// <summary>A single equalizer band.</summary>
    public struct Band {
        /// <summary>Position of the band.</summary>
        public readonly double Frequency { get; }
        /// <summary>Gain at <see cref="Frequency"/> in dB.</summary>
        public readonly double Gain { get; }

        /// <summary>EQ band constructor.</summary>
        public Band(double frequency, double gain) {
            Frequency = frequency;
            Gain = gain;
        }
    }
}
