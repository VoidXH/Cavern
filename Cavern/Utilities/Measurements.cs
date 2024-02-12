using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Tools for measuring frequency response.
    /// </summary>
    public static partial class Measurements {
        /// <summary>
        /// Fast Fourier transform a 2D signal. The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] FFT(this Complex[] samples) {
            using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
            return samples.FFT(cache);
        }

        /// <summary>
        /// Fast Fourier transform a 2D signal.
        /// </summary>
        public static Complex[] FFT(this Complex[] samples, FFTCache cache) {
            samples = samples.FastClone();
            samples.InPlaceFFT(cache);
            return samples;
        }

        /// <summary>
        /// Fast Fourier transform many 2D signals.
        /// </summary>
        public static Complex[][] FFT(this Complex[][] samples, bool parallel) {
            Complex[][] result = new Complex[samples.Length][];
            if (parallel) {
                using FFTCache cache = new ThreadSafeFFTCache(samples[0].Length);
                for (int i = 0; i < result.Length; i++) {
                    result[i] = samples[i].FFT(cache);
                }
            } else {
                using FFTCachePool pool = new FFTCachePool(samples[0].Length);
                Parallelizer.For(0, result.Length, i => {
                    FFTCache cache = pool.Lease();
                    result[i] = samples[i].FFT(cache);
                    pool.Return(cache);
                });
            }
            return result;
        }

        /// <summary>
        /// Fast Fourier transform a 1D signal. The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] FFT(this float[] samples) {
            using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
            return samples.FFT(cache);
        }

        /// <summary>
        /// Fast Fourier transform a 1D signal.
        /// </summary>
        public static Complex[] FFT(this float[] samples, FFTCache cache) {
            Complex[] complexSignal = new Complex[samples.Length];
            for (int sample = 0; sample < samples.Length; sample++) {
                complexSignal[sample].Real = samples[sample];
            }
            complexSignal.InPlaceFFT(cache);
            return complexSignal;
        }

        /// <summary>
        /// Fast Fourier transform a 2D signal while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceFFT(this Complex[] samples) {
            if (CavernAmp.Available && CavernAmp.IsMono()) {
                CavernAmp.InPlaceFFT(samples);
            } else {
                using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
                ProcessFFT(samples, cache);
            }
        }

        /// <summary>
        /// Fast Fourier transform a 2D signal while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceFFT(this Complex[] samples, FFTCache cache) {
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceFFT(samples, cache);
            } else {
                ProcessFFT(samples, cache);
            }
        }

        /// <summary>
        /// Spectrum of a signal's FFT. The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static float[] FFT1D(this float[] samples) {
            using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
            return samples.FFT1D(cache);
        }

        /// <summary>
        /// Spectrum of a signal's FFT.
        /// </summary>
        public static float[] FFT1D(this float[] samples, FFTCache cache) {
            samples = samples.FastClone();
            samples.InPlaceFFT(cache);
            return samples;
        }

        /// <summary>
        /// Spectrum of a signal's FFT while keeping the source array allocation.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceFFT(this float[] samples) {
            if (CavernAmp.Available && CavernAmp.IsMono()) {
                CavernAmp.InPlaceFFT(samples);
            } else {
                using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
                ProcessFFT(samples, cache);
            }
        }

        /// <summary>
        /// Spectrum of a signal's FFT while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceFFT(this float[] samples, FFTCache cache) {
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceFFT(samples, cache);
            } else {
                ProcessFFT(samples, cache);
            }
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] IFFT(this Complex[] samples) {
            samples = samples.FastClone();
            if (CavernAmp.Available && CavernAmp.IsMono()) {
                CavernAmp.InPlaceIFFT(samples);
            } else {
                using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
                samples.InPlaceIFFT(cache);
            }
            return samples;
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal.
        /// </summary>
        public static Complex[] IFFT(this Complex[] samples, FFTCache cache) {
            samples = samples.FastClone();
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceIFFT(samples, cache);
            } else {
                samples.InPlaceIFFT(cache);
            }
            return samples;
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static void InPlaceIFFT(this Complex[] samples) {
            if (CavernAmp.Available && CavernAmp.IsMono()) {
                CavernAmp.InPlaceIFFT(samples);
                return;
            }
            using FFTCache cache = new ThreadSafeFFTCache(samples.Length);
            samples.InPlaceIFFT(cache);
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.
        /// </summary>
        public static unsafe void InPlaceIFFT(this Complex[] samples, FFTCache cache) {
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceIFFT(samples, cache);
                return;
            }
            ProcessIFFT(samples, cache);
            float multiplier = 1f / samples.Length;
            fixed (Complex* pSamples = samples) {
                Complex* sample = pSamples,
                    end = sample + samples.Length;
                while (sample != end) {
                    *sample++ *= multiplier;
                }
            }
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation, without a
        /// division with the number of elements. This is the definition of IFFT, but unsuitable for measurement use.
        /// </summary>
        public static void InPlaceIFFTUnscaled(this Complex[] samples, FFTCache cache) {
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceIFFT(samples, cache);
                for (int i = 0; i < samples.Length; i++) {
                    samples[i].Real *= samples.Length;
                    samples[i].Imaginary *= samples.Length;
                }
                return;
            }
            ProcessIFFT(samples, cache);
        }

        /// <summary>
        /// Minimizes the phase of a spectrum. The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        /// <remarks>This function does not handle zeros in the spectrum.
        /// Make sure there is a threshold before using this function.</remarks>
        public static void MinimumPhaseSpectrum(Complex[] response) => MinimumPhaseSpectrum(response, null);

        /// <summary>
        /// Minimizes the phase of a spectrum.
        /// </summary>
        /// <remarks>This function does not handle zeros in the spectrum.
        /// Make sure there is a threshold before using this function.</remarks>
        public static void MinimumPhaseSpectrum(Complex[] response, FFTCache cache) {
            bool customCache = false;
            if (cache == null) {
                cache = new ThreadSafeFFTCache(response.Length);
                customCache = true;
            }
            int halfLength = response.Length / 2;
            for (int i = 0; i < response.Length; i++) {
                response[i] = Complex.Log(response[i].Real);
            }
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceIFFT(response, cache);
            } else {
                response.InPlaceIFFT(cache);
            }
            for (int i = 1; i < halfLength; i++) {
                response[i].Real += response[^i].Real;
                response[i].Imaginary -= response[^i].Imaginary;
                response[^i].Clear();
            }
            response[halfLength].Imaginary = -response[halfLength].Imaginary;
            if (cache != null && cache.Native != IntPtr.Zero) {
                CavernAmp.InPlaceFFT(response, cache);
            } else {
                response.InPlaceFFT(cache);
            }
            for (int i = 0; i < response.Length; i++) {
                float exp = MathF.Exp(response[i].Real);
                response[i].Real = exp * MathF.Cos(response[i].Imaginary);
                response[i].Imaginary = exp * MathF.Sin(response[i].Imaginary);
            }
            if (customCache) {
                cache.Dispose();
            }
        }

        /// <summary>
        /// Add gain to every frequency except a given band.
        /// </summary>
        public static void OffbandGain(Complex[] samples, double startFreq, double endFreq, double sampleRate, double dBgain) {
            int startPos = (int)(samples.Length * startFreq / sampleRate),
                endPos = (int)(samples.Length * endFreq / sampleRate);
            float gain = (float)Math.Pow(10, dBgain * .05);
            samples[0] *= gain;
            for (int i = 1; i < startPos; i++) {
                samples[i] *= gain;
                samples[^i] *= gain;
            }
            for (int i = endPos + 1, half = samples.Length / 2; i <= half; i++) {
                samples[i] *= gain;
                samples[^i] *= gain;
            }
        }

        /// <summary>
        /// Get the real part of a signal's FFT.
        /// </summary>
        public static float[] GetRealPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; sample++) {
                output[sample] = samples[sample].Real;
            }
            return output;
        }

        /// <summary>
        /// Get the real part of a signal's FFT.
        /// </summary>
        public static unsafe void GetRealPart(this Complex[] samples, float[] output) {
            fixed (Complex* pSamples = samples)
            fixed (float* pOutput = output) {
                Complex* inSample = pSamples,
                    end = inSample + Math.Min(samples.Length, output.Length);
                float* outSample = pOutput;
                while (inSample != end) {
                    *outSample++ = inSample++->Real;
                }
            }
        }

        /// <summary>
        /// Get half of the real part of a signal's FFT.
        /// </summary>
        public static float[] GetRealPartHalf(Complex[] samples) {
            float[] output = new float[samples.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = samples[sample].Real;
            }
            return output;
        }

        /// <summary>
        /// Get the imaginary part of a signal's FFT.
        /// </summary>
        public static float[] GetImaginaryPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; sample++) {
                output[sample] = samples[sample].Imaginary;
            }
            return output;
        }

        /// <summary>
        /// Get the gains of frequencies in a signal after FFT.
        /// </summary>
        public static float[] GetSpectrum(Complex[] samples) {
            float[] output = new float[samples.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = samples[sample].Magnitude;
            }
            return output;
        }

        /// <summary>
        /// Get the gains of frequencies in a signal after FFT.
        /// </summary>
        public static float[] GetPhase(Complex[] samples) {
            float[] output = new float[samples.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = samples[sample].Phase;
            }
            return output;
        }

        /// <summary>
        /// Get the frequency response using the original sweep signal's FFT as reference.
        /// </summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, Complex[] responseFFT) {
            responseFFT.Deconvolve(referenceFFT);
            return responseFFT;
        }

        /// <summary>
        /// Get the frequency response using the original sweep signal's FFT as reference.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, float[] response) =>
            GetFrequencyResponse(referenceFFT, response.FFT());

        /// <summary>
        /// Get the frequency response using the original sweep signal's FFT as reference.
        /// </summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, float[] response, FFTCache cache) =>
            GetFrequencyResponse(referenceFFT, response.FFT(cache));

        /// <summary>
        /// Get the frequency response using the original sweep signal as reference.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] GetFrequencyResponse(float[] reference, float[] response) {
            using FFTCache tempCache = new ThreadSafeFFTCache(reference.Length);
            return GetFrequencyResponse(reference, response, tempCache);
        }

        /// <summary>
        /// Get the frequency response using the original sweep signal as reference.
        /// </summary>
        public static Complex[] GetFrequencyResponse(float[] reference, float[] response, FFTCache cache) =>
            GetFrequencyResponse(reference.FFT(cache), response.FFT(cache));

        /// <summary>
        /// Get the complex impulse response using a precalculated frequency response.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        public static Complex[] GetImpulseResponse(Complex[] frequencyResponse) => frequencyResponse.IFFT();

        /// <summary>
        /// Get the complex impulse response using a precalculated frequency response.
        /// </summary>
        public static Complex[] GetImpulseResponse(Complex[] frequencyResponse, FFTCache cache) => frequencyResponse.IFFT(cache);

        /// <summary>
        /// Get the complex impulse response using the original sweep signal as a reference.
        ///
        /// </summary>
        public static Complex[] GetImpulseResponse(float[] reference, float[] response) =>
            IFFT(GetFrequencyResponse(reference, response));

        /// <summary>
        /// Get the complex impulse response using the original sweep signal as a reference.
        /// </summary>
        public static Complex[] GetImpulseResponse(float[] reference, float[] response, FFTCache cache) =>
            IFFT(GetFrequencyResponse(reference, response), cache);

        /// <summary>
        /// FFT processor selector.
        /// </summary>
        static void ProcessFFT(Complex[] samples, FFTCache cache) {
            if (samples.Length > 8) {
                if (CavernAmp.IsMono()) {
                    ProcessFFT_Mono(samples, cache, QMath.Log2(samples.Length) - 1);
                } else {
                    ProcessFFT(samples, cache, QMath.Log2(samples.Length) - 1);
                }
            } else {
                ProcessFFTSmall(samples);
            }
        }

        /// <summary>
        /// Outputs IFFT(X) * N.
        /// </summary>
        static void ProcessIFFT(Complex[] samples, FFTCache cache) {
            samples.Conjugate();
            ProcessFFT(samples, cache);
            samples.Conjugate();
        }
    }
}