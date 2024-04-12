using System;

namespace Cavern.Utilities {
    public static partial class Measurements {
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
    }
}