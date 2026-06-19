using System;

using Cavern.Filters;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Methods for calculating delays of signals, be it in impulse response or transfer function format.
    /// </summary>
    public static class DelayCalculation {
        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by a determined <paramref name="method"/>.
        /// </summary>
        public static float Get(float[] impulseResponse, DelayDeterminationType method) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(impulseResponse),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(impulseResponse),
            DelayDeterminationType.HilbertPeak => GetHilbertPeakDelay(impulseResponse),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by a determined <paramref name="method"/>.
        /// </summary>
        public static float Get(Complex[] transferFunction, DelayDeterminationType method) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(transferFunction),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(transferFunction),
            DelayDeterminationType.HilbertPeak => GetHilbertPeakDelay(transferFunction),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(float[] impulseResponse) => GetSlopeDelay(impulseResponse.FFT());

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the highest absolute value sample.
        /// </summary>
        public static int GetImpulsePeakDelay(float[] impulseResponse) {
            int result = 0;
            float absPeak = Math.Abs(impulseResponse[0]), absHere;
            for (int pos = 1; pos < impulseResponse.Length; pos++) {
                absHere = Math.Abs(impulseResponse[pos]);
                if (absPeak < absHere) {
                    absPeak = absHere;
                    result = pos;
                }
            }
            return result;
        }

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the highest absolute value sample of the impulse response's Hilbert transform.
        /// </summary>
        public static int GetHilbertPeakDelay(float[] impulseResponse) {
            float[] impulse = impulseResponse.FastClone();
            using PhaseShifter phaseShifter = new PhaseShifter(impulseResponse.Length, true);
            phaseShifter.Process(impulse);
            return GetImpulsePeakDelay(impulse);
        }

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(Complex[] transferFunction) {
            float[] result = Measurements.GetPhase(transferFunction);
            (double slope, double _) = GraphUtils.GetRegression(result);
            return (float)(-slope * transferFunction.Length / (2 * Math.PI));
        }

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the highest absolute value sample of the impulse response.
        /// </summary>
        public static int GetImpulsePeakDelay(Complex[] transferFunction) {
            float[] impulse = Measurements.GetRealIFFT(transferFunction);
            return GetImpulsePeakDelay(impulse);
        }

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the highest absolute value sample of the impulse response's Hilbert transform.
        /// </summary>
        public static int GetHilbertPeakDelay(Complex[] transferFunction) {
            float[] impulse = Measurements.GetRealIFFT(transferFunction);
            using PhaseShifter phaseShifter = new PhaseShifter(impulse.Length, true);
            phaseShifter.Process(impulse);
            return GetImpulsePeakDelay(impulse);
        }
    }
}
