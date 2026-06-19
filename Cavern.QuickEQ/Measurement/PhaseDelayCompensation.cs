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
        /// <param name="type">Used delay compensation method</param>
        /// <param name="unwrap">Return unwrapped phase</param>
        public static float[] GetUndelayedPhase(Complex[] transferFunction, DelayDeterminationType type, bool unwrap) {
            float delay = DelayCalculation.Get(transferFunction, type);
            if (delay != 0) {
                WaveformUtils.Delay(transferFunction, -delay);
            }

            float[] result = Measurements.GetPhase(transferFunction);
            if (unwrap) {
                Measurements.UnwrapPhase(result);
            }
            return result;
        }
    }
}
