using System;

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
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(impulseResponse),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by a determined <paramref name="method"/>. Better performance is achieved with an <see cref="FFTCache"/>.
        /// </summary>
        public static float Get(float[] impulseResponse, DelayDeterminationType method, FFTCache cache) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(impulseResponse, cache),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(impulseResponse),
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(impulseResponse),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by a determined <paramref name="method"/>.
        /// </summary>
        public static float Get(Complex[] transferFunction, DelayDeterminationType method) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(transferFunction),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(transferFunction),
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(transferFunction),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by a determined <paramref name="method"/>. Better performance is achieved with an <see cref="FFTCache"/>.
        /// </summary>
        public static float Get(Complex[] transferFunction, DelayDeterminationType method, FFTCache cache) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(transferFunction),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(transferFunction, cache),
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(transferFunction, cache),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(float[] impulseResponse) => GetSlopeDelay(impulseResponse.FFT());

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(float[] impulseResponse, FFTCache cache) => GetSlopeDelay(impulseResponse.FFT(cache));

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
        /// Get the delay of an <paramref name="impulseResponse"/> by the highest absolute value sample of the impulse response's envelope.
        /// </summary>
        public static int GetImpulseEnvelopePeakDelay(float[] impulseResponse) {
            float[] envelope = Measurements.GetEnvelope(impulseResponse);
            return GetImpulsePeakDelay(envelope);
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
        public static int GetImpulsePeakDelay(Complex[] transferFunction) => GetImpulsePeakDelay(transferFunction.GetRealIFFT());

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the highest absolute value sample of the impulse response.
        /// </summary>
        public static int GetImpulsePeakDelay(Complex[] transferFunction, FFTCache cache) => GetImpulsePeakDelay(transferFunction.GetRealIFFT(cache));

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the highest absolute value sample of the impulse response's envelope.
        /// </summary>
        public static int GetImpulseEnvelopePeakDelay(Complex[] transferFunction) {
            using FFTCache cache = new FFTCache(transferFunction.Length);
            return GetImpulseEnvelopePeakDelay(transferFunction, cache);
        }

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by the highest absolute value sample of the impulse response's envelope.
        /// </summary>
        public static int GetImpulseEnvelopePeakDelay(Complex[] transferFunction, FFTCache cache) {
            float[] impulseResponse = transferFunction.GetRealIFFT(cache);
            return GetImpulseEnvelopePeakDelay(impulseResponse);
        }
    }
}
