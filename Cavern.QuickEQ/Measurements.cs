using Cavern.Utilities;
using System;

namespace Cavern.QuickEQ {
    /// <summary>Tools for measuring frequency response.</summary>
    public static class Measurements {
        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        static void ProcessFFT(Complex[] samples, FFTCache cache, int depth) {
            int halfLength = samples.Length / 2;
            if (samples.Length == 1)
                return;
            Complex[] even = cache.Even[depth], odd = cache.Odd[depth];
            for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
                even[sample] = samples[pair];
                odd[sample] = samples[pair + 1];
            }
            ProcessFFT(even, cache, --depth);
            ProcessFFT(odd, cache, depth);
            int stepMul = cache.cos.Length / halfLength;
            for (int i = 0; i < halfLength; ++i) {
                float oddReal = odd[i].Real * cache.cos[i * stepMul] - odd[i].Imaginary * cache.sin[i * stepMul],
                    oddImag = odd[i].Real * cache.sin[i * stepMul] + odd[i].Imaginary * cache.cos[i * stepMul];
                samples[i].Real = even[i].Real + oddReal;
                samples[i].Imaginary = even[i].Imaginary + oddImag;
                samples[i + halfLength].Real = even[i].Real - oddReal;
                samples[i + halfLength].Imaginary = even[i].Imaginary - oddImag;
            }
        }

        /// <summary>Fourier-transform a signal in 1D. The result is the spectral power.</summary>
        static void ProcessFFT(float[] samples, FFTCache cache) {
            int halfLength = samples.Length / 2, depth = QMath.Log2(samples.Length) - 1;
            if (samples.Length == 1)
                return;
            Complex[] even = cache.Even[depth], odd = cache.Odd[depth];
            for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
                even[sample].Real = samples[pair];
                odd[sample].Real = samples[pair + 1];
            }
            ProcessFFT(even, cache, --depth);
            ProcessFFT(odd, cache, depth);
            int stepMul = cache.cos.Length / halfLength;
            for (int i = 0; i < halfLength; ++i) {
                float oddReal = odd[i].Real * cache.cos[i * stepMul] - odd[i].Imaginary * cache.sin[i * stepMul],
                    oddImag = odd[i].Real * cache.sin[i * stepMul] + odd[i].Imaginary * cache.cos[i * stepMul];
                float real = even[i].Real + oddReal, imaginary = even[i].Imaginary + oddImag;
                samples[i] = (float)Math.Sqrt(real * real + imaginary * imaginary);
                real = even[i].Real - oddReal;
                imaginary = even[i].Imaginary - oddImag;
                samples[i + halfLength] = (float)Math.Sqrt(real * real + imaginary * imaginary);
            }
        }

        /// <summary>Fast Fourier transform a 2D signal.</summary>
        public static Complex[] FFT(Complex[] samples, FFTCache cache = null) {
            samples = (Complex[])samples.Clone();
            if (cache == null)
                cache = new FFTCache(samples.Length);
            ProcessFFT(samples, cache, QMath.Log2(samples.Length) - 1);
            return samples;
        }

        /// <summary>Fast Fourier transform a 1D signal.</summary>
        public static Complex[] FFT(float[] samples, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(samples.Length);
            Complex[] complexSignal = new Complex[samples.Length];
            for (int sample = 0; sample < samples.Length; ++sample)
                complexSignal[sample].Real = samples[sample];
            ProcessFFT(complexSignal, cache, QMath.Log2(samples.Length) - 1);
            return complexSignal;
        }

        /// <summary>Fast Fourier transform a 2D signal while keeping the source array allocation.</summary>
        public static void InPlaceFFT(Complex[] samples, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(samples.Length);
            ProcessFFT(samples, cache, QMath.Log2(samples.Length) - 1);
        }

        /// <summary>Spectrum of a signal's FFT.</summary>
        public static float[] FFT1D(float[] samples, FFTCache cache = null) {
            samples = (float[])samples.Clone();
            if (cache == null)
                cache = new FFTCache(samples.Length);
            ProcessFFT(samples, cache);
            return samples;
        }

        /// <summary>Spectrum of a signal's FFT while keeping the source array allocation.</summary>
        public static void InPlaceFFT(float[] samples, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(samples.Length);
            ProcessFFT(samples, cache);
        }

        /// <summary>Outputs IFFT(X) * N.</summary>
        static void ProcessIFFT(Complex[] samples, FFTCache cache, int depth) {
            if (samples.Length == 1)
                return;
            Complex[] even = cache.Even[depth], odd = cache.Odd[depth];
            int halfLength = samples.Length / 2;
            for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
                even[sample] = samples[pair];
                odd[sample] = samples[pair + 1];
            }
            ProcessIFFT(even, cache, --depth);
            ProcessIFFT(odd, cache, depth);
            int stepMul = cache.cos.Length / halfLength;
            for (int i = 0; i < halfLength; ++i) {
                float oddReal = odd[i].Real * cache.cos[i * stepMul] - odd[i].Imaginary * -cache.sin[i * stepMul],
                    oddImag = odd[i].Real * -cache.sin[i * stepMul] + odd[i].Imaginary * cache.cos[i * stepMul];
                samples[i].Real = even[i].Real + oddReal;
                samples[i].Imaginary = even[i].Imaginary + oddImag;
                samples[i + halfLength].Real = even[i].Real - oddReal;
                samples[i + halfLength].Imaginary = even[i].Imaginary - oddImag;
            }
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal.</summary>
        public static Complex[] IFFT(Complex[] samples, FFTCache cache = null) {
            samples = (Complex[])samples.Clone();
            if (cache == null)
                cache = new FFTCache(samples.Length);
            InPlaceIFFT(samples, cache);
            return samples;
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.</summary>
        public static void InPlaceIFFT(Complex[] samples, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(samples.Length);
            ProcessIFFT(samples, cache, QMath.Log2(samples.Length) - 1);
            float multiplier = 1f / samples.Length;
            for (int i = 0; i < samples.Length; ++i) {
                samples[i].Real *= multiplier;
                samples[i].Imaginary *= multiplier;
            }
        }

        /// <summary>Minimizes the phase of a spectrum.</summary>
        /// <remarks>This function does not handle zeros in the spectrum. Make sure there is a threshold before using this function.</remarks>
        public static void MinimumPhaseSpectrum(Complex[] response, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(response.Length);
            int halfLength = response.Length / 2;
            for (int i = 0; i < response.Length; ++i) {
                response[i].Real = (float)Math.Log(response[i].Real);
                response[i].Imaginary = 0;
            }
            InPlaceIFFT(response, cache);
            for (int i = 1; i < halfLength; ++i) {
                response[i].Real += response[response.Length - i].Real;
                response[i].Imaginary -= response[response.Length - i].Imaginary;
                response[response.Length - i].Real = 0;
                response[response.Length - i].Imaginary = 0;
            }
            response[halfLength].Imaginary = -response[halfLength].Imaginary;
            InPlaceFFT(response, cache);
            for (int i = 0; i < response.Length; ++i) {
                double exp = Math.Exp(response[i].Real);
                response[i].Real = (float)(exp * Math.Cos(response[i].Imaginary));
                response[i].Imaginary = (float)(exp * Math.Sin(response[i].Imaginary));
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
            for (int i = 1; i < startPos; ++i) {
                samples[i] *= gain;
                samples[samples.Length - i] *= gain;
            }
            for (int i = endPos + 1, half = samples.Length / 2; i <= half; ++i) {
                samples[i] *= gain;
                samples[samples.Length - i] *= gain;
            }
        }

        /// <summary>Get the real part of a signal's FFT.</summary>
        public static float[] GetRealPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; ++sample)
                output[sample] = samples[sample].Real;
            return output;
        }

        /// <summary>Get half of the real part of a signal's FFT.</summary>
        public static float[] GetRealPartHalf(Complex[] samples) {
            int half = samples.Length / 2;
            float[] output = new float[half];
            for (int sample = 0; sample < half; ++sample)
                output[sample] = samples[sample].Real;
            return output;
        }

        /// <summary>Get the imaginary part of a signal's FFT.</summary>
        public static float[] GetImaginaryPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; ++sample)
                output[sample] = samples[sample].Imaginary;
            return output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetSpectrum(Complex[] samples) {
            int end = samples.Length / 2;
            float[] output = new float[end];
            for (int sample = 0; sample < end; ++sample)
                output[sample] = samples[sample].Magnitude;
            return output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetPhase(Complex[] samples) {
            int end = samples.Length / 2;
            float[] output = new float[end];
            for (int sample = 0; sample < end; ++sample)
                output[sample] = samples[sample].Phase;
            return output;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, Complex[] responseFFT) {
            for (int sample = 0; sample < responseFFT.Length; ++sample)
                responseFFT[sample].Divide(ref referenceFFT[sample]);
            return responseFFT;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, float[] response, FFTCache cache = null) =>
            GetFrequencyResponse(referenceFFT, FFT(response, cache));

        /// <summary>Get the frequency response using the original sweep signal as reference.</summary>
        public static Complex[] GetFrequencyResponse(float[] reference, float[] response, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(reference.Length);
            return GetFrequencyResponse(FFT(reference, cache), FFT(response, cache));
        }

        /// <summary>Get the complex impulse response using a precalculated frequency response.</summary>
        public static Complex[] GetImpulseResponse(Complex[] frequencyResponse, FFTCache cache = null) => IFFT(frequencyResponse, cache);

        /// <summary>Get the complex impulse response using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] reference, float[] response, FFTCache cache = null) =>
            IFFT(GetFrequencyResponse(reference, response), cache);
    }
}