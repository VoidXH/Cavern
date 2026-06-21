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
        /// <param name="windowing">Window the impulse to +/- this many samples around the found peak before getting its phase curve, 0 disables this feature</param>
        public static float[] GetUndelayedPhase(Complex[] transferFunction, DelayDeterminationType type, bool unwrap, int windowing) {
            FFTCache cache = new FFTCache(transferFunction.Length);
            return GetUndelayedPhase(transferFunction, type, unwrap, windowing, cache);
        }

        /// <summary>
        /// Get a phase curve for a given <paramref name="transferFunction"/> without distortions caused by delay.
        /// </summary>
        /// <param name="transferFunction">Input transfer function to parse the phase of</param>
        /// <param name="type">Used delay compensation method</param>
        /// <param name="unwrap">Return unwrapped phase</param>
        /// <param name="windowing">Window the impulse to +/- this many samples around the found peak before getting its phase curve, 0 disables this feature</param>
        /// <param name="cache">Preallocated optimization data for faster processing</param>
        public static float[] GetUndelayedPhase(Complex[] transferFunction, DelayDeterminationType type, bool unwrap, int windowing, FFTCache cache) {
            float delay = DelayCalculation.Get(transferFunction, type, cache);
            if (delay != 0) {
                if (windowing != 0) {
                    transferFunction.InPlaceIFFT(cache);
                    int center = (int)delay;
                    Windowing.ApplyWindow(transferFunction, Window.Tukey, Window.Tukey, center - windowing, center, center + windowing);
                    transferFunction.InPlaceFFT(cache);
                }
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
