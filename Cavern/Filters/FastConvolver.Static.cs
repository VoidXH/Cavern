using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    public partial class FastConvolver {
        /// <summary>
        /// Performs the convolution of two real signals. The real result is returned.
        /// The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/>
        /// to match in a length of a power of 2.</remarks>
        public static float[] Convolve(float[] excitation, float[] impulse) {
            Complex[] result = ConvolveFourier(excitation, impulse);
            result.InPlaceIFFT();
            return Measurements.GetRealPart(result);
        }

        /// <summary>
        /// Performs the convolution of two real signals. The real result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/>
        /// to match in a length of a power of 2.</remarks>
        public static float[] Convolve(float[] excitation, float[] impulse, FFTCache cache) {
            Complex[] result = ConvolveFourier(excitation, impulse, cache);
            result.InPlaceIFFT();
            return Measurements.GetRealPart(result);
        }

        /// <summary>
        /// Performs the convolution of two real signals. The FFT of the result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/>
        /// to match in a length of a power of 2.</remarks>
        public static Complex[] ConvolveFourier(float[] excitation, float[] impulse) {
            using FFTCache cache = new FFTCache(excitation.Length);
            Complex[] excitationFFT = excitation.FFT(cache);
            excitationFFT.Convolve(impulse.FFT(cache));
            return excitationFFT;
        }

        /// <summary>
        /// Performs the convolution of two real signals. The FFT of the result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/>
        /// to match in a length of a power of 2.</remarks>
        public static Complex[] ConvolveFourier(float[] excitation, float[] impulse, FFTCache cache) {
            Complex[] excitationFFT = excitation.FFT(cache);
            excitationFFT.Convolve(impulse.FFT(cache));
            return excitationFFT;
        }

        /// <summary>
        /// Performs the convolution of two real signals of any length. The real result is returned.
        /// </summary>
        public static float[] ConvolveSafe(float[] excitation, float[] impulse) {
            using FFTCache tempCache = new ThreadSafeFFTCache(1 << QMath.Log2Ceil(excitation.Length + impulse.Length));
            return ConvolveSafe(excitation, impulse, tempCache);
        }

        /// <summary>
        /// Performs the convolution of two real signals of any length. The real result is returned.
        /// </summary>
        /// <remarks>The size of the <paramref name="cache"/> has to equal
        /// 2 ^ ceil(log2(<paramref name="excitation"/>.Length + <paramref name="impulse"/>.Length)).</remarks>
        public static float[] ConvolveSafe(float[] excitation, float[] impulse, FFTCache cache) {
            int finalLength = excitation.Length + impulse.Length;
            float[] real = new float[1 << QMath.Log2Ceil(finalLength)];
            Array.Copy(excitation, real, excitation.Length);
            Complex[] excitationFFT = real.FFT(cache);

            Array.Copy(impulse, real, impulse.Length);
            Array.Clear(real, impulse.Length, real.Length - impulse.Length);
            Complex[] impulseFFT = real.FFT(cache);

            float[] result = new float[finalLength];
            excitationFFT.Convolve(impulseFFT);
            excitationFFT.InPlaceIFFT();
            excitationFFT.GetRealPart(result);
            return result;
        }
    }
}