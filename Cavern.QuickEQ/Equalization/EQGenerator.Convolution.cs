using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class EQGenerator {
        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will not be applied, and the filter's length will be the default of 1024 samples.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetConvolution(this Equalizer eq, int sampleRate) =>
            eq.GetConvolution(sampleRate, 1024, 1, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will not be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length) =>
            eq.GetConvolution(sampleRate, length, 1, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length, float gain) =>
            eq.GetConvolution(sampleRate, length, gain, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will not be applied, and the filter's length will be the default of 1024 samples.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="cache">Reused FFT helper values for better performance - use the non-cache version for auto-calculation</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, FFTCache cache) =>
            eq.GetConvolution(sampleRate, 1, null, cache);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// The initial curve can be provided in Fourier-space.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length, float gain, Complex[] initial) {
            using FFTCache cache = new ThreadSafeFFTCache(length << 1);
            return eq.GetConvolution(sampleRate, gain, initial, cache);
        }

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// The initial curve can be provided in Fourier-space.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be the same size as the <paramref name="cache"/></param>
        /// <param name="cache">Reused FFT helper values for better performance - use the non-cache version for auto-calculation</param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, float gain, Complex[] initial, FFTCache cache) {
            int length = cache.Size;
            Complex[] filter = new Complex[cache.Size];
            if (initial == null) {
                for (int i = 0; i < length; i++) {
                    filter[i].Real = gain; // FFT of DiracDelta(x)
                }
            } else {
                for (int i = 0; i < length; i++) {
                    filter[i].Real = initial[i].Magnitude * gain;
                }
            }
            eq.Apply(filter, sampleRate);
            Measurements.MinimumPhaseSpectrum(filter, cache);
            filter.InPlaceIFFT(cache);

            // Hann windowing for increased precision
            float mul = 2 * MathF.PI / length;
            length >>= 1;
            for (int i = 0; i < length; i++) {
                filter[i].Real *= .5f * (1 + MathF.Cos(i * mul));
            }
            return Measurements.GetRealPartHalf(filter);
        }

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will not be applied, and the filter's length will be the default of 1024 samples.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate) =>
            eq.GetLinearConvolution(sampleRate, 1024, 1, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will not be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length) =>
            eq.GetLinearConvolution(sampleRate, length, 1, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length, float gain) =>
            eq.GetLinearConvolution(sampleRate, length, gain, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// The initial curve can be provided in Fourier-space.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length, float gain, Complex[] initial) {
            Complex[] filter = new Complex[length];
            if (initial == null) {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = (i & 1) == 0 ? gain : -gain; // FFT of DiracDelta(x - length/2)
                }
            } else {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = initial[i].Magnitude * ((i & 1) == 0 ? gain : -gain);
                }
            }
            eq.Apply(filter, sampleRate);
            filter.InPlaceIFFT();
            return Measurements.GetRealPart(filter);
        }
    }
}
