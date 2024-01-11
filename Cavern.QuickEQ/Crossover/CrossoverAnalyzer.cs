using Cavern.Utilities;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tools for analyzing and tuning measured or generated crossovers.
    /// </summary>
    public static class CrossoverAnalyzer {
        /// <summary>
        /// Gets the optimal frequency to put the crossover point at by simulation.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="lowTransfer">Transfer function of the low-frequency path</param>
        /// <param name="highTransfer">Transfer function of the high-frequency path</param>
        /// <param name="sampleRate">Sample rate where the transfer functions were recorded</param>
        /// <param name="minFreq">Minimum allowed crossover frequency</param>
        /// <param name="maxFreq">Maximum allowed crossover frequency</param>
        /// <param name="precision">Steps between checked crossover frequencies</param>
        /// <remarks>This function doesn't account for the 10 dB gain of LFE channels as it could be used for determining the
        /// crossover point of multiway speakers too.</remarks>
        public static float FindCrossoverFrequency(Crossover type, Complex[] lowTransfer, Complex[] highTransfer, int sampleRate,
            float minFreq, float maxFreq, float precision) {
            using FFTCache cache = new ThreadSafeFFTCache(lowTransfer.Length);
            return FindCrossoverFrequency(type, lowTransfer, highTransfer, sampleRate, minFreq, maxFreq, precision, cache);
        }

        /// <summary>
        /// Gets the optimal frequency to put the crossover point at by simulation.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="lowTransfer">Transfer function of the low-frequency path</param>
        /// <param name="highTransfer">Transfer function of the high-frequency path</param>
        /// <param name="sampleRate">Sample rate where the transfer functions were recorded</param>
        /// <param name="minFreq">Minimum allowed crossover frequency</param>
        /// <param name="maxFreq">Maximum allowed crossover frequency</param>
        /// <param name="precision">Steps between checked crossover frequencies</param>
        /// <param name="cache">Preallocated FFT cache for optimization</param>
        /// <remarks>This function doesn't account for the 10 dB gain of LFE channels as it could be used for determining the
        /// crossover point of multiway speakers too.</remarks>
        public static float FindCrossoverFrequency(Crossover type, Complex[] lowTransfer, Complex[] highTransfer, int sampleRate,
            float minFreq, float maxFreq, float precision, FFTCache cache) {
            float bestValue = 0,
                bestFrequency = 0;
            for (float freq = minFreq; freq <= maxFreq; freq += precision) {
                Complex[] lowCurrent = lowTransfer.FastClone();
                Complex[] highCurrent = highTransfer.FastClone();
                lowCurrent.Convolve(type.GetLowpass(sampleRate, freq, cache.Size).FFT(cache));
                highCurrent.Convolve(type.GetHighpass(sampleRate, freq, cache.Size).FFT(cache));
                lowCurrent.Add(highCurrent);
                float value = lowCurrent.GetRMSMagnitude();
                if (bestValue < value) {
                    bestValue = value;
                    bestFrequency = freq;
                }
            }
            return bestFrequency;
        }
    }
}