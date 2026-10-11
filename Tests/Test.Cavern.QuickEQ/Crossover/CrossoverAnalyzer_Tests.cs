using Cavern.QuickEQ.Crossover;
using Cavern.QuickEQ.Crossover.Enums;
using Cavern.Utilities;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tests the <see cref="CrossoverAnalyzer"/> class.
    /// </summary>
    [TestClass]
    public class CrossoverAnalyzer_Tests {
        /// <summary>
        /// Runs a calibration-tolerance crossover frequency test with default parameters.
        /// </summary>
        /// <param name="fftSize">The FFT size.</param>
        /// <param name="divergenceBin">The bin index where the high transfer diverges, or -1 for no divergence.</param>
        /// <param name="divergenceMagnitude">The magnitude of the diverged bin.</param>
        static float FindCalibratedFrequency(int fftSize, int divergenceBin = -1, float divergenceMagnitude = 0f) {
            Complex[] low = new Complex[fftSize];
            Complex[] high = new Complex[fftSize];
            for (int i = 0; i < fftSize; i++) {
                low[i] = new Complex(1f, 0f);
                high[i] = new Complex(1f, 0f);
            }
            if (divergenceBin >= 0) {
                high[divergenceBin] = new Complex(divergenceMagnitude, 0f);
            }

            CrossoverAnalyzer analyzer = new(CrossoverType.Biquad, 1000, 2, CrossoverSlope.Butterworth) {
                MinFreq = 40,
                MaxFreq = 100,
                Precision = 10,
                CalibrationTolerance = 3
            };

            using FFTCache cache = new(fftSize);
            return analyzer.FindCrossoverFrequency(low, high, cache);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverAnalyzer.FindCrossoverFrequency"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FindCrossoverFrequency() {
            BasicCrossover crossover = Crossovers.Basic5_1;
            using FFTCache cache = new(512);
            const int sampleRate = 500;
            Complex[] high = crossover.GetHighpass(sampleRate, 80, cache.Size).FFT(cache),
                low = crossover.GetLowpass(sampleRate, 80, cache.Size).FFT(cache);
            CrossoverAnalyzer analyzer = new(CrossoverType.Biquad, sampleRate, 2, CrossoverSlope.Butterworth) {
                MinFreq = 40,
                MaxFreq = 120,
                Precision = 10
            };
            float freq = analyzer.FindCrossoverFrequency(low, high, cache);
            Assert.AreEqual(80, freq);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverAnalyzer.FindCrossoverFrequency"/> returns <see cref="CrossoverAnalyzer.MinFreq"/>
        /// when <see cref="CrossoverAnalyzer.CalibrationTolerance"/> is set but the low/high transfer functions
        /// never diverge above the tolerance within the checked range.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FindCrossoverFrequency_CalibrationTolerance_NoDivergence() {
            float freq = FindCalibratedFrequency(fftSize: 512);
            Assert.AreEqual(40f, freq);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverAnalyzer.FindCrossoverFrequency"/> returns the divergence frequency
        /// when <see cref="CrossoverAnalyzer.CalibrationTolerance"/> is set and the low/high transfer functions
        /// diverge above <see cref="CrossoverAnalyzer.MinFreq"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FindCrossoverFrequency_CalibrationTolerance_DivergenceAboveMinFreq() {
            float freq = FindCalibratedFrequency(fftSize: 1024, divergenceBin: 49, divergenceMagnitude: 0.01f);
            Assert.AreEqual(50f, freq);
        }
    }
}
