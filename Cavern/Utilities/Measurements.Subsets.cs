using System;

using Cavern.Filters;

namespace Cavern.Utilities {
    public static partial class Measurements {
        /// <summary>
        /// Get the envelope of an <paramref name="impulseResponse"/>.
        /// </summary>
        public static float[] GetEnvelope(float[] impulseResponse) {
            float[] hilbert = PhaseShifter.PhaseShiftInPlace(impulseResponse, true);
            Complex[] analyticSignal = ComplexArray.Merge(impulseResponse, hilbert);
            return GetMagnitude(analyticSignal);
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
        /// Get the magnitudes of bins in a <paramref name="transferFunction"/>.
        /// This function is not optimal for spectral analysis, that would be <see cref="GetSpectrum(Complex[])"/>.
        /// </summary>
        public static float[] GetMagnitude(Complex[] transferFunction) {
            float[] output = new float[transferFunction.Length];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = transferFunction[sample].Magnitude;
            }
            return output;
        }

        /// <summary>
        /// Get the angles of frequencies in a signal after FFT.
        /// </summary>
        public static float[] GetPhase(Complex[] samples) {
            float[] output = new float[samples.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = samples[sample].Phase;
            }
            return output;
        }

        /// <summary>
        /// Get the real part of a complex signal after performing IFFT on it.
        /// </summary>
        public static float[] GetRealIFFT(this Complex[] samples) {
            Complex[] workingTF = samples.FastClone();
            IFFT(workingTF);
            return GetRealPart(workingTF);
        }

        /// <summary>
        /// Get the real part of a complex signal after performing IFFT on it.
        /// </summary>
        public static float[] GetRealIFFT(this Complex[] samples, FFTCache cache) {
            Complex[] workingTF = samples.FastClone();
            IFFT(workingTF, cache);
            return GetRealPart(workingTF);
        }

        /// <summary>
        /// Get the real part of a complex signal (most commonly an IFFT result after Fourier-space operations).
        /// </summary>
        public static float[] GetRealPart(Complex[] samples) {
            float[] output = new float[samples.Length];
            for (int sample = 0; sample < samples.Length; sample++) {
                output[sample] = samples[sample].Real;
            }
            return output;
        }

        /// <summary>
        /// Get the real part of a complex signal (most commonly an IFFT result after Fourier-space operations).
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
        /// Get half of the real part of a complex signal (most commonly an IFFT result after Fourier-space operations).
        /// </summary>
        public static float[] GetRealPartHalf(Complex[] samples) {
            float[] output = new float[samples.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = samples[sample].Real;
            }
            return output;
        }

        /// <summary>
        /// Get the gains of frequencies in of a signal's FFT (<paramref name="transferFunction"/>).
        /// The bands are only returned up to half the frequency scale, since for spectra, the FFT is symmetric.
        /// For operations that require all bins, use <see cref="GetMagnitude(Complex[])"/>.
        /// </summary>
        public static float[] GetSpectrum(Complex[] transferFunction) {
            float[] output = new float[transferFunction.Length / 2];
            for (int sample = 0; sample < output.Length; sample++) {
                output[sample] = transferFunction[sample].Magnitude;
            }
            return output;
        }
    }
}
