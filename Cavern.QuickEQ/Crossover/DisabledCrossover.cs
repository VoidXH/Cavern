namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A crossover that performs no filtering. Exported as a bypass, with no crossover frequencies.
    /// </summary>
    public sealed class DisabledCrossover : Crossover {
        /// <summary>
        /// Creates a crossover that performs no filtering.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public DisabledCrossover(CrossoverDescription mixing) : base(mixing, CrossoverType.Disabled) { }
    }
}
