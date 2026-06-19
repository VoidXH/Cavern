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
        public static float[] CorrectDelay(Complex[] transferFunction, int sampleRate, DelayDeterminationType type,
            double startFrequency, double detectionFrequency, double endFrequency, bool unwrap) {
            int delay = 0;
            if (type == DelayDeterminationType.ImpulsePeak) {
                delay = DelayCalculation.GetImpulsePeakDelay(transferFunction);
            } else if (type == DelayDeterminationType.HilbertPeak) {
                delay = DelayCalculation.GetHilbertPeakDelay(transferFunction);
            }
            if (delay != 0) {
                WaveformUtils.Delay(transferFunction, -delay);
            }

            float[] result = Measurements.GetPhase(transferFunction);
            if (type == DelayDeterminationType.Slope) {
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
    }
}
