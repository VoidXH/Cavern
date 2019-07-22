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
                int cachePos = i * stepMul;
                float oddReal = odd[i].Real * cache.cos[cachePos] - odd[i].Imaginary * cache.sin[cachePos],
                    oddImag = odd[i].Real * cache.sin[cachePos] + odd[i].Imaginary * cache.cos[cachePos];
                samples[i].Real = even[i].Real + oddReal;
                samples[i].Imaginary = even[i].Imaginary + oddImag;
                int o = i + halfLength;
                samples[o].Real = even[i].Real - oddReal;
                samples[o].Imaginary = even[i].Imaginary - oddImag;
            }
        }

        /// <summary>Fourier-transform a signal in 1D. The result is the spectral power.</summary>
        static void ProcessFFT(float[] samples, FFTCache cache) {
            int halfLength = samples.Length / 2, depth = Utils.Log2(samples.Length) - 1;
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
                int cachePos = i * stepMul;
                float oddReal = odd[i].Real * cache.cos[cachePos] - odd[i].Imaginary * cache.sin[cachePos],
                    oddImag = odd[i].Real * cache.sin[cachePos] + odd[i].Imaginary * cache.cos[cachePos];
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
            ProcessFFT(samples, cache ?? new FFTCache(samples.Length), Utils.Log2(samples.Length) - 1);
            return samples;
        }

        /// <summary>Fast Fourier transform a 1D signal.</summary>
        public static Complex[] FFT(float[] samples, FFTCache cache = null) {
            Complex[] complexSignal = new Complex[samples.Length];
            for (int sample = 0; sample < samples.Length; ++sample)
                complexSignal[sample].Real = samples[sample];
            ProcessFFT(complexSignal, cache ?? new FFTCache(samples.Length), Utils.Log2(samples.Length) - 1);
            return complexSignal;
        }

        /// <summary>Fast Fourier transform a 2D signal while keeping the source array allocation.</summary>
        public static void InPlaceFFT(Complex[] samples, FFTCache cache = null) =>
            ProcessFFT(samples, cache ?? new FFTCache(samples.Length), Utils.Log2(samples.Length) - 1);

        /// <summary>Spectrum of a signal's FFT.</summary>
        public static float[] FFT1D(float[] samples, FFTCache cache = null) {
            samples = (float[])samples.Clone();
            ProcessFFT(samples, cache ?? new FFTCache(samples.Length));
            return samples;
        }

        /// <summary>Spectrum of a signal's FFT while keeping the source array allocation.</summary>
        public static void InPlaceFFT(float[] samples, FFTCache cache = null) => ProcessFFT(samples, cache ?? new FFTCache(samples.Length));

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
                int cachePos = i * stepMul;
                float oddReal = odd[i].Real * cache.cos[cachePos] - odd[i].Imaginary * -cache.sin[cachePos],
                    oddImag = odd[i].Real * -cache.sin[cachePos] + odd[i].Imaginary * cache.cos[cachePos];
                samples[i].Real = even[i].Real + oddReal;
                samples[i].Imaginary = even[i].Imaginary + oddImag;
                int o = i + halfLength;
                samples[o].Real = even[i].Real - oddReal;
                samples[o].Imaginary = even[i].Imaginary - oddImag;
            }
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal.</summary>
        public static Complex[] IFFT(Complex[] samples, FFTCache cache = null) {
            samples = (Complex[])samples.Clone();
            InPlaceIFFT(samples, cache ?? new FFTCache(samples.Length));
            return samples;
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.</summary>
        public static void InPlaceIFFT(Complex[] samples, FFTCache cache) {
            ProcessIFFT(samples, cache, Utils.Log2(samples.Length) - 1);
            float multiplier = 1f / samples.Length;
            for (int i = 0; i < samples.Length; ++i) {
                samples[i].Real *= multiplier;
                samples[i].Imaginary *= multiplier;
            }
        }

        /// <summary>Get the real part of a signal's FFT.</summary>
        public static float[] GetRealPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; ++sample)
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

        /// <summary>Generate a linear frequency sweep with a flat frequency response.</summary>
        public static float[] LinearSweep(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = (endFreq - startFreq) / (2 * samples / (double)sampleRate);
            for (int sample = 0; sample < samples; ++sample) {
                double position = sample / (double)sampleRate;
                output[sample] = (float)Math.Sin(2 * Math.PI * (startFreq * position + chirpyness * position * position));
            }
            return output;
        }

        /// <summary>Generate the frequencies at each sample's position in a linear frequency sweep.</summary>
        public static float[] LinearSweepFreqs(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] freqs = new float[samples];
            float chirpyness = endFreq - startFreq / (samples / (float)sampleRate);
            for (int sample = 0; sample < samples; ++sample)
                freqs[sample] = startFreq + chirpyness * sample / sampleRate;
            return freqs;
        }

        /// <summary>Generate an exponential frequency sweep.</summary>
        public static float[] ExponentialSweep(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = Math.Pow(endFreq / startFreq, sampleRate / (double)samples),
                logChirpyness = Math.Log(chirpyness), sinConst = 2 * Math.PI * startFreq;
            for (int sample = 0; sample < samples; ++sample)
                output[sample] = (float)Math.Sin(sinConst * (Math.Pow(chirpyness, sample / (double)sampleRate) - 1) / logChirpyness);
            return output;
        }

        /// <summary>Generate the frequencies at each sample's position in an exponential frequency sweep.</summary>
        public static float[] ExponentialSweepFreqs(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] freqs = new float[samples];
            double chirpyness = Math.Pow(endFreq / startFreq, sampleRate / (double)samples);
            for (int sample = 0; sample < samples; ++sample)
                freqs[sample] = startFreq + (float)Math.Pow(chirpyness, sample / (double)sampleRate);
            return freqs;
        }

        /// <summary>Add silence to the beginning and the end of a sweep for a larger response window.</summary>
        public static float[] SweepFraming(float[] sweep) {
            int initialSilence = sweep.Length / 4;
            float[] result = new float[sweep.Length * 2];
            for (int sample = initialSilence, end = sweep.Length + initialSilence; sample < end; ++sample)
                result[sample] = sweep[sample - initialSilence];
            return result;
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