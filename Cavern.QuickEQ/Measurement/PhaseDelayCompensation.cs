using System;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Methods to compensate for delay's effects in a phase curve.
    /// </summary>
    public static class PhaseDelayCompensation {
        /// <summary>
        /// Get a phase curve for a given <paramref name="transferFunction"/> without distortions caused by delay.
        /// </summary>
        /// <param name="transferFunction">Input transfer function to parse the phase of</param>
        /// <param name="sampleRate">Sampling rate of the <paramref name="transferFunction"/></param>
        /// <param name="type">Used delay compensation method</param>
        /// <param name="startFrequency">First frequency to consider for frequency-limited compensation types</param>
        /// <param name="detectionFrequency">Optimally the last frequency to consider for frequency-limited compensation types</param>
        /// <param name="endFrequency">The transfer function's last valid frequency to consider for frequency-limited compensation types</param>
        /// <param name="unwrap">Return unwrapped phase</param>
        public static float[] CorrectDelay(Complex[] transferFunction, int sampleRate, PhaseDelayCompensationType type,
            double startFrequency, double detectionFrequency, double endFrequency, bool unwrap) {
            if (type == PhaseDelayCompensationType.ImpulsePeak) {
                transferFunction = CompensateForImpulsePeak(transferFunction);
            }
            float[] result = Measurements.GetPhase(transferFunction);
            if (type == PhaseDelayCompensationType.Slope) {
                Measurements.UnwrapPhase(result);
                CompensateForSlope(result, startFrequency, detectionFrequency, endFrequency, sampleRate);
                if (!unwrap) {
                    Measurements.WrapPhase(result);
                }
            } else if (unwrap) {
                Measurements.UnwrapPhase(result);
            }
            return result;
        }

        /// <summary>
        /// Remove the delay's effects from the <paramref name="phase"/> curve by removing its slope.
        /// </summary>
        static void CompensateForSlope(float[] phase, double startFrequency, double detectionFrequency, double endFrequency, int sampleRate) {
            (double slope, double intercept) = GraphUtils.GetRegression(phase, (int)(phase.Length * startFrequency / sampleRate),
                (int)(phase.Length * Math.Min(detectionFrequency, endFrequency) / sampleRate));
            for (int i = 0; i < phase.Length; i++) {
                phase[i] -= (float)(intercept + slope * i);
            }
        }

        /// <summary>
        /// Remove the delay's effects from the initial <paramref name="transferFunction"/> by adding a delay that moves the impulse peak to the origin.
        /// </summary>
        static Complex[] CompensateForImpulsePeak(Complex[] transferFunction) {
            Complex[] delayCalculator = transferFunction.FastClone();
            Measurements.IFFT(delayCalculator);
            int delay = new VerboseImpulseResponse(delayCalculator).Delay;
            Complex[] result = transferFunction.FastClone();
            WaveformUtils.Delay(result, -delay);
            return result;
        }
    }
}
