using Cavern.QuickEQ.Crossover;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Crossovers preallocated to be used for crossover tests.
    /// </summary>
    static class Crossovers {
        /// <summary>
        /// A basic 4.2 crossover where all channels are mixed to the LFE at the same <see cref="freq"/>uency.
        /// </summary>
        public static BasicCrossover Basic4_2 => basic4_2 ??= new(Description4_2);
        static BasicCrossover basic4_2;

        /// <summary>
        /// A basic 5.1 crossover where all channels are mixed to the LFE at the same <see cref="freq"/>uency.
        /// </summary>
        public static BasicCrossover Basic5_1 => basic5_1 ??= new(Description5_1);
        static BasicCrossover basic5_1;

        /// <summary>
        /// Description of a 4.2 crossover where all channels are mixed to both LFE at the same <see cref="freq"/>uency.
        /// </summary>
        static CrossoverDescription Description4_2 => description4_2 ??= new((false, freq), (false, freq), (false, freq), (false, freq), (true, 0), (true, 0));
        static CrossoverDescription description4_2;

        /// <summary>
        /// Description of a 5.1 crossover where all channels are mixed to the LFE at the same <see cref="freq"/>uency.
        /// </summary>
        static CrossoverDescription Description5_1 => description5_1 ??= new((false, freq), (false, freq), (false, freq), (true, 0), (false, freq), (false, freq));
        static CrossoverDescription description5_1;

        /// <summary>
        /// Default crossover frequency used for preallocated crossovers.
        /// </summary>
        const float freq = 80;
    }
}
