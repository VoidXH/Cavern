using System;

namespace Cavern.Utilities {
    // Measurement functions related to phase curves
    public static partial class Measurements {
        /// <summary>
        /// Minimizes the phase of a spectrum. The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        /// <remarks>This function does not handle zeros in the spectrum.
        /// Make sure there is a <see cref="ComplexArray.Threshold(Complex[], float)"/> before using this function.</remarks>
        public static void ConvertToMinimumPhase(Complex[] response) => ConvertToMinimumPhase(response, null);

        /// <summary>
        /// Minimizes the phase of a spectrum.
        /// </summary>
        /// <remarks>This function does not handle zeros in the spectrum.
        /// Make sure there is a <see cref="ComplexArray.Threshold(Complex[], float)"/> before using this function.</remarks>
        public static void ConvertToMinimumPhase(Complex[] response, FFTCache cache) {
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
        /// Try to recover the actual <paramref name="phase"/> curve from the results that are confined to the unit circle.
        /// </summary>
        public static void UnwrapPhase(float[] phase) {
            float addition = 0, last = phase[0];
            for (int i = 1; i < phase.Length; i++) {
                float diff = phase[i] - last;
                last = phase[i];
                if (Math.Abs(diff) > MathF.PI) {
                    addition -= tau * Math.Sign(diff);
                }
                phase[i] += addition;
            }
        }

        /// <summary>
        /// Force a phase curve between the [-pi; pi] bounds.
        /// </summary>
        public static void WrapPhase(float[] phase) {
            for (int i = 0; i < phase.Length; i++) {
                phase[i] = (phase[i] + MathF.PI) % tau;
                if (phase[i] < 0) {
                    phase[i] += tau;
                }
                phase[i] -= MathF.PI;
            }
        }

        /// <summary>
        /// Unit circle constant.
        /// </summary>
        const float tau = 2 * MathF.PI;
    }
}
