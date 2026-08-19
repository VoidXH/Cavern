using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Sweeping {
    /// <summary>
    /// Calculates the frequency and impulse responses in the background while further sweeps are being calculated.
    /// </summary>
    internal class SweeperBackgroundCalculator {
        /// <summary>
        /// Calculated spectrum of the measured channel or channel group.
        /// </summary>
        public Equalizer FrequencyResponse { get; private set; }

        /// <summary>
        /// Calculated impulse response of the measured channel or channel group.
        /// </summary>
        public VerboseImpulseResponse ImpulseResponse { get; private set; }

        /// <summary>
        /// Calculates the frequency and impulse responses in the background while further sweeps are being calculated.
        /// </summary>
        public SweeperBackgroundCalculator(Complex[] sweepFFT, FFTCache sweepFFTCache, float[] response, int sampleRate, double filterMains) {
            if (filterMains != 0) {
                Notch.CreateForMainsHum(filterMains, sampleRate, 15).Process(response);
            }
            Complex[] rawResponse = Measurements.GetFrequencyResponse(sweepFFT, response.FFT(sweepFFTCache));
            FrequencyResponse = EQGenerator.FromTransferFunctionOptimized(rawResponse, sampleRate);
            ImpulseResponse = new VerboseImpulseResponse(Measurements.GetImpulseResponse(rawResponse, sweepFFTCache));
        }
    }
}
