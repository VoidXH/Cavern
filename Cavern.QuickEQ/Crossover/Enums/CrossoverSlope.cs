namespace Cavern.QuickEQ.Crossover.Enums {
    /// <summary>
    /// Standard crossover slope types with their corresponding Q-factors for 2nd-order sections.
    /// </summary>
    public enum CrossoverSlope {
        /// <summary>
        /// Butterworth alignment (maximally flat magnitude response).
        /// Q-factor: 0.7071 (1/√2) per 2nd-order section.
        /// </summary>
        Butterworth = 0,

        /// <summary>
        /// Linkwitz-Riley alignment (flat summed response, 6 dB down at crossover).
        /// Q-factor: 0.5 per 2nd-order section.
        /// </summary>
        LinkwitzRiley = 1,

        /// <summary>
        /// Bessel alignment (maximally flat group delay/linear phase).
        /// Q-factor: 0.57735 (1/√3) per 2nd-order section.
        /// </summary>
        Bessel = 2
    }
}
