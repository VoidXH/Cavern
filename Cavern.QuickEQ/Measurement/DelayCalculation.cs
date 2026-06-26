using System;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;
using Cavern.Utilities.Threading;
using Cavern.Waveforms;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Methods for calculating delays of signals, be it in impulse response or transfer function format.
    /// </summary>
    public static class DelayCalculation {
        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by a determined <paramref name="method"/>.
        /// </summary>
        public static float Get(float[] impulseResponse, DelayDeterminationType method) {
            using FFTCache cache = new FFTCache(impulseResponse.Length);
            return Get(impulseResponse, method, cache);
        }

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by a determined <paramref name="method"/>. Better performance is achieved with an <see cref="FFTCache"/>.
        /// </summary>
        public static float Get(float[] impulseResponse, DelayDeterminationType method, FFTCache cache) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(impulseResponse, cache),
            DelayDeterminationType.SlopeWindowed => GetSlopeWindowedDelay(impulseResponse),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(impulseResponse),
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(impulseResponse),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by a determined <paramref name="method"/>.
        /// </summary>
        public static float Get(Complex[] transferFunction, DelayDeterminationType method) {
            using FFTCache cache = new FFTCache(transferFunction.Length);
            return Get(transferFunction, method, cache);
        }

        /// <summary>
        /// Get the delay of a <paramref name="transferFunction"/> by a determined <paramref name="method"/>. Better performance is achieved with an <see cref="FFTCache"/>.
        /// </summary>
        public static float Get(Complex[] transferFunction, DelayDeterminationType method, FFTCache cache) => method switch {
            DelayDeterminationType.None => 0,
            DelayDeterminationType.Slope => GetSlopeDelay(transferFunction),
            DelayDeterminationType.SlopeWindowed => GetSlopeWindowedDelay(transferFunction, cache),
            DelayDeterminationType.ImpulsePeak => GetImpulsePeakDelay(transferFunction, cache),
            DelayDeterminationType.ImpulseEnvelopePeak => GetImpulseEnvelopePeakDelay(transferFunction, cache),
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Get the delay of each channel of a <paramref name="multichannel"/> impulse response by a determined <paramref name="method"/>.
        /// </summary>
        public static float[] Get(MultichannelWaveform multichannel, DelayDeterminationType method, FFTCachePool pool, bool multithreaded) {
            float[] result = new float[multichannel.Channels];
            Parallelizer.ForUnchecked(0, multichannel.Channels, i => {
                FFTCache cache = pool.Lease();
                result[i] = Get(multichannel[i], method, cache);
                pool.Return(cache);
            }, multithreaded);
            return result;
        }

        /// <summary>
        /// Get the delay of each channel of a <paramref name="multichannel"/> transfer function by a determined <paramref name="method"/>.
        /// </summary>
        public static float[] Get(MultichannelTransferFunction multichannel, DelayDeterminationType method, FFTCachePool pool, bool multithreaded) {
            float[] result = new float[multichannel.Channels];
            Parallelizer.ForUnchecked(0, multichannel.Channels, i => {
                FFTCache cache = pool.Lease();
                result[i] = Get(multichannel[i], method, cache);
                pool.Return(cache);
            }, multithreaded);
            return result;
        }

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(float[] impulseResponse) => GetSlopeDelay(impulseResponse.FFT());

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response.
        /// </summary>
        public static float GetSlopeDelay(float[] impulseResponse, FFTCache cache) => GetSlopeDelay(impulseResponse.FFT(cache));

        /// <summary>
        /// Get the delay of an <paramref name="impulseResponse"/> by the slope of the phase response after the phase is windowed to the envelope peak's +/-64 sample area.
        /// This is practically the same result as <see cref="DelayDeterminationType.ImpulseEnvelopePeak"/>, but with subsample precision.
        /// </summary>
        public static float GetSlopeWindowedDelay(float[] impulseResponse) {
            int delay = GetImpulseEnvelopePeakDelay(impulseResponse);
            float[] windowed = impulseResponse.FastClone();
            Windowing.ApplyWindow(windowed, Window.Tukey, Window.Tukey, delay - 64, delay, delay + 64);
            return GetSlopeDelay(windowed);
        }

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
        /// Get the delay of a <paramref name="transferFunction"/> by the slope of the phase response in samples.
        /// </summary>
        public static float GetSlopeDelay(Complex[] transferFunction) {
            float[] result = Measurements.GetPhase(transferFunction);
            Measurements.UnwrapPhase(result);
            (double slope, double _) = GraphUtils.GetRegression(result);
            double delaySamples = -slope * transferFunction.Length / (2 * Math.PI);
            return (float)(delaySamples < 0 ? transferFunction.Length + delaySamples : delaySamples);
        }

        /// <summary>
        /// Get the delay of  a <paramref name="transferFunction"/> by the slope of the phase response after the phase is windowed to the envelope peak's +/-64 sample area.
        /// This is practically the same result as <see cref="DelayDeterminationType.ImpulseEnvelopePeak"/>, but with subsample precision.
        /// </summary>
        public static float GetSlopeWindowedDelay(Complex[] transferFunction) {
            using FFTCache cache = new FFTCache(transferFunction.Length);
            return GetSlopeWindowedDelay(transferFunction, cache);
        }

        /// <summary>
        /// Get the delay of  a <paramref name="transferFunction"/> by the slope of the phase response after the phase is windowed to the envelope peak's +/-64 sample area.
        /// This is practically the same result as <see cref="DelayDeterminationType.ImpulseEnvelopePeak"/>, but with subsample precision.
        /// </summary>
        public static float GetSlopeWindowedDelay(Complex[] transferFunction, FFTCache cache) {
            int delay = GetImpulseEnvelopePeakDelay(transferFunction);
            float[] windowed = transferFunction.GetRealIFFT(cache);
            Windowing.ApplyWindow(windowed, Window.Tukey, Window.Tukey, delay - 64, delay, delay + 64);
            return GetSlopeDelay(windowed);
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
