using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Tools for measuring frequency response.</summary>
    public static class Measurements {
        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        static void ProcessFFT(Complex[] samples, FFTCache cache, int depth) {
            int length = samples.Length, halfLength = length / 2;
            if (length == 1)
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
            int length = samples.Length, halfLength = length / 2, depth = CavernUtilities.Log2(samples.Length) - 1;
            if (length == 1)
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
                samples[i] = Mathf.Sqrt(real * real + imaginary * imaginary);
                real = even[i].Real - oddReal;
                imaginary = even[i].Imaginary - oddImag;
                samples[i + halfLength] = Mathf.Sqrt(real * real + imaginary * imaginary);
            }
        }

        /// <summary>Fast Fourier transform a 2D signal.</summary>
        public static Complex[] FFT(Complex[] samples, FFTCache cache = null) {
            samples = (Complex[])samples.Clone();
            ProcessFFT(samples, cache ?? new FFTCache(samples.Length), CavernUtilities.Log2(samples.Length) - 1);
            return samples;
        }

        /// <summary>Fast Fourier transform a 1D signal.</summary>
        public static Complex[] FFT(float[] samples, FFTCache cache = null) {
            int length = samples.Length;
            Complex[] complexSignal = new Complex[length];
            for (int sample = 0; sample < length; ++sample)
                complexSignal[sample].Real = samples[sample];
            ProcessFFT(complexSignal, cache ?? new FFTCache(samples.Length), CavernUtilities.Log2(length) - 1);
            return complexSignal;
        }

        /// <summary>Fast Fourier transform a 2D signal while keeping the source array allocation.</summary>
        public static void InPlaceFFT(Complex[] samples, FFTCache cache = null) =>
            ProcessFFT(samples, cache ?? new FFTCache(samples.Length), CavernUtilities.Log2(samples.Length) - 1);

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
            int length = samples.Length, halfLength = length / 2;
            if (length == 1)
                return;
            Complex[] even = cache.Even[depth], odd = cache.Odd[depth];
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
            int length = samples.Length;
            ProcessIFFT(samples, cache, CavernUtilities.Log2(length) - 1);
            float multiplier = 1f / length;
            for (int i = 0; i < length; ++i) {
                samples[i].Real *= multiplier;
                samples[i].Imaginary *= multiplier;
            }
        }

        /// <summary>Get the real part of a signal's FFT.</summary>
        public static float[] GetRealPart(Complex[] samples) {
            int end = samples.Length;
            float[] output = new float[end];
            for (int sample = 0; sample < end; ++sample)
                output[sample] = samples[sample].Real;
            return output;
        }

        /// <summary>Get the imaginary part of a signal's FFT.</summary>
        public static float[] GetImaginaryPart(Complex[] samples) {
            int end = samples.Length;
            float[] output = new float[end];
            for (int sample = 0; sample < end; ++sample)
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
            float chirpyness = (endFreq - startFreq) / (2 * samples / (float)sampleRate);
            for (int sample = 0; sample < samples; ++sample) {
                float position = sample / (float)sampleRate;
                output[sample] = Mathf.Sin(2 * Mathf.PI * (startFreq * position + chirpyness * position * position));
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
            float chirpyness = Mathf.Pow(endFreq / startFreq, sampleRate / (float)samples),
                logChirpyness = Mathf.Log(chirpyness), sinConst = 2 * Mathf.PI * startFreq;
            for (int sample = 0; sample < samples; ++sample)
                output[sample] = Mathf.Sin(sinConst * (Mathf.Pow(chirpyness, sample / (float)sampleRate) - 1) / logChirpyness);
            return output;
        }

        /// <summary>Generate the frequencies at each sample's position in an exponential frequency sweep.</summary>
        public static float[] ExponentialSweepFreqs(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] freqs = new float[samples];
            float chirpyness = Mathf.Pow(endFreq / startFreq, sampleRate / (float)samples);
            for (int sample = 0; sample < samples; ++sample)
                freqs[sample] = startFreq + Mathf.Pow(chirpyness, sample / (float)sampleRate);
            return freqs;
        }

        /// <summary>Add silence to the beginning and the end of a sweep for a larger response window.</summary>
        public static float[] SweepFraming(float[] sweep) {
            int length = sweep.Length, initialSilence = length / 4;
            float[] result = new float[length * 2];
            for (int sample = initialSilence, end = length + initialSilence; sample < end; ++sample)
                result[sample] = sweep[sample - initialSilence];
            return result;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, Complex[] responseFFT) {
            for (int sample = 0, length = responseFFT.Length; sample < length; ++sample)
                responseFFT[sample].Divide(ref referenceFFT[sample]);
            return responseFFT;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] referenceFFT, float[] response, FFTCache cache = null) =>
            GetFrequencyResponse(referenceFFT, FFT(response, cache));

        /// <summary>Get the frequency response using the original sweep signal as reference.</summary>
        public static Complex[] GetFrequencyResponse(float[] reference, float[] response, FFTCache cache = null) =>
            GetFrequencyResponse(FFT(reference, cache), response);

        /// <summary>Get the complex impulse response using a precalculated frequency response.</summary>
        public static Complex[] GetImpulseResponse(Complex[] frequencyResponse, FFTCache cache = null) => IFFT(frequencyResponse, cache);

        /// <summary>Get the complex impulse response using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] reference, float[] response, FFTCache cache = null) =>
            IFFT(GetFrequencyResponse(reference, response), cache);

        /// <summary>Convert a response curve to decibel scale.</summary>
        public static void ConvertToDecibels(float[] curve, float minimum = -100) {
            for (int i = 0, end = curve.Length; i < end; ++i) {
                curve[i] = 20 * Mathf.Log10(curve[i]);
                if (curve[i] < minimum)
                    curve[i] = minimum;
            }
        }

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
        /// <param name="samples">Source response</param>
        /// <param name="startFreq">Frequency at the first position of the output</param>
        /// <param name="endFreq">Frequency at the last position of the output</param>
        /// <param name="sampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="resultSize">Length of the resulting array</param>
        public static float[] ConvertToGraph(float[] samples, float startFreq, float endFreq, int sampleRate, int resultSize) {
            float sourceSize = samples.Length - 1, positioner = sourceSize * 2 / sampleRate, powerMin = Mathf.Log10(startFreq),
                powerRange = (Mathf.Log10(endFreq) - powerMin) / resultSize; // Divide 'i' here, not ResultScale times
            float[] graph = new float[resultSize];
            for (int i = 0; i < resultSize; ++i) {
                float freqHere = Mathf.Pow(10, powerMin + powerRange * i);
                graph[i] = samples[(int)(freqHere * positioner)];
            }
            return graph;
        }

        /// <summary>Apply smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], float, float, int, int)"/>.</summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float octave = 1 / 3f) {
            if (octave == 0)
                return (float[])samples.Clone();
            float octaveRange = Mathf.Log(endFreq, 2) - Mathf.Log(startFreq, 2);
            int length = samples.Length, windowSize = (int)(length * octave / octaveRange), lastBlock = length - windowSize;
            float[] smoothed = new float[length--];
            float average = 0;
            for (int sample = 0; sample < windowSize; ++sample)
                average += samples[sample];
            for (int sample = 0; sample < windowSize; ++sample) {
                int newSample = sample + windowSize;
                average += samples[newSample];
                smoothed[sample] = average / newSample;
            }
            for (int sample = windowSize; sample < lastBlock; ++sample) {
                int oldSample = sample - windowSize, newSample = sample + windowSize;
                average += samples[newSample] - samples[oldSample];
                smoothed[sample] = average / (newSample - oldSample);
            }
            for (int sample = lastBlock; sample <= length; ++sample) {
                int oldSample = sample - windowSize;
                average -= samples[oldSample];
                smoothed[sample] = average / (length - oldSample);
            }
            return smoothed;
        }

        /// <summary>Apply variable smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], float, float, int, int)"/>.</summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float startOctave, float endOctave) {
            float[] startGraph = SmoothGraph(samples, startFreq, endFreq, startOctave), endGraph = SmoothGraph(samples, startFreq, endFreq, endOctave),
                output = new float[samples.Length];
            float positioner = 1f / samples.Length;
            for (int i = 0, length = samples.Length; i < length; ++i)
                output[i] = Utils.Lerp(startGraph[i], endGraph[i], i * positioner);
            return output;
        }
    }
}