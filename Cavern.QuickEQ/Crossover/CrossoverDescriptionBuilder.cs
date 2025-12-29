namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Methods for building <see cref="CrossoverDescription"/>s.
    /// </summary>
    public static class CrossoverDescriptionBuilder {
        /// <summary>
        /// Describe a <see cref="Crossover"/> that has separate crossover frequencies for screen and surround channels.
        /// </summary>
        /// <param name="layout">Channel layout of the user</param>
        /// <param name="screenCrossover">Crossover frequency for screen channels, or 0 to bypass screen channel crossovers</param>
        /// <param name="surroundCrossover">Crossover frequency for surround channels, or 0 to bypass surround channel crossovers</param>
        public static CrossoverDescription Basic(Channel[] layout, float screenCrossover, float surroundCrossover) {
            (bool mixFrom, float freq)[] result = new (bool, float)[layout.Length];
            for (int channel = 0; channel < result.Length; channel++) {
                if (layout[channel].LFE) {
                    result[channel].mixFrom = true;
                } else {
                    result[channel].freq = layout[channel].IsScreenChannel ? screenCrossover : surroundCrossover;
                }
            }
            return new CrossoverDescription(result);
        }
    }
}
