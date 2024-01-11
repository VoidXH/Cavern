using Cavern.Utilities;

using CrossoverBase = Cavern.QuickEQ.Crossover.Crossover;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Helper functions for testing Crossover derivatives.
    /// </summary>
    static class Utils {
        public static void ImpulseResponse(CrossoverBase crossover, float expectedHighpassValue, float expectedLowpassValue) {
            const int fftSize = 256,
                sampleRate = 48000,
                crossoverFreq = 10000,
                crossoverBand = fftSize * crossoverFreq / sampleRate;
            FFTCache cache = new(fftSize);
            Complex[] highpass = crossover.GetHighpass(sampleRate, crossoverFreq, fftSize).FFT(cache);
            Complex[] lowpass = crossover.GetLowpass(sampleRate, crossoverFreq, fftSize).FFT(cache);

            float highpassAtCrossover = highpass[crossoverBand].Magnitude,
                lowpassAtCrossover = lowpass[crossoverBand].Magnitude;
            Assert.IsTrue(highpassAtCrossover - highpass[crossoverBand + 1].Magnitude < 0);
            Assert.AreEqual(expectedHighpassValue, highpassAtCrossover);
            Assert.IsTrue(lowpassAtCrossover - lowpass[crossoverBand + 1].Magnitude > 0);
            Assert.AreEqual(expectedLowpassValue, lowpassAtCrossover);
        }
    }
}