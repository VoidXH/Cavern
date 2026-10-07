namespace Cavern.QuickEQ.Crossover.Enums {
    /// <summary>
    /// Supported types of crossovers.
    /// </summary>
    public enum CrossoverType {
        /// <summary>
        /// No crossover is applied.
        /// </summary>
        Disabled,

        /// <summary>
        /// Crossover made of generic 2nd order highpass/lowpass filters.
        /// </summary>
        Biquad,

        /// <summary>
        /// Brickwall FIR crossover.
        /// </summary>
        Cavern,

        /// <summary>
        /// FIR realization of <see cref="Biquad"/>, without any phase distortions.
        /// </summary>
        SyntheticBiquad
    }
}
