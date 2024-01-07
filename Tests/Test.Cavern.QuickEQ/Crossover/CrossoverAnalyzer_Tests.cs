using Cavern.QuickEQ.Crossover;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tests the <see cref="CrossoverAnalyzer"/> class.
    /// </summary>
    [TestClass]
    public class CrossoverAnalyzer_Tests {
        /// <summary>
        /// Tests if <see cref="CrossoverAnalyzer.FindCrossoverFrequency"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FindCrossoverFrequency() {
            BasicCrossover crossover = new(null, null);
            using FFTCache cache = new(512);
            const int sampleRate = 500;
            Complex[] high = crossover.GetHighpass(sampleRate, 80, cache.Size).FFT(cache),
                low = crossover.GetLowpass(sampleRate, 80, cache.Size).FFT(cache);
            float freq = CrossoverAnalyzer.FindCrossoverFrequency(crossover, low, high, sampleRate, 40, 120, 10);
            Assert.AreEqual(70, freq);
        }
    }
}