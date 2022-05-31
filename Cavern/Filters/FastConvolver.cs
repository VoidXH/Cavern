using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs an optimized convolution.
    /// </summary>
    /// <remarks>This filter is using the overlap and add method using FFTs.</remarks>
    public class FastConvolver : Filter {
        /// <summary>
        /// Created convolution filter in Fourier-space.
        /// </summary>
        readonly Complex[] filter;

        /// <summary>
        /// Cache to perform the FFT in.
        /// </summary>
        readonly Complex[] present;

        /// <summary>
        /// Overlap samples from previous runs.
        /// </summary>
        readonly float[] future;

        /// <summary>
        /// FFT optimization.
        /// </summary>
        readonly FFTCache cache;

        /// <summary>
        /// Delay applied with the convolution.
        /// </summary>
        readonly int delay;

        /// <summary>
        /// Constructs an optimized convolution.
        /// </summary>
        public FastConvolver(float[] impulse, int delay = 0) {
            int fftSize = 2 << QMath.Log2Ceil(impulse.Length); // Zero padding for the falloff to have space
            cache = new FFTCache(fftSize);
            filter = new Complex[fftSize];
            for (int sample = 0; sample < impulse.Length; ++sample)
                filter[sample].Real = impulse[sample];
            filter.InPlaceFFT(cache);
            present = new Complex[fftSize];
            future = new float[fftSize + delay];
            this.delay = delay;
        }

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            int start = 0;
            while (start < samples.Length)
                ProcessTimeslot(samples, start, Math.Min(samples.Length, start += filter.Length >> 1));
        }

        /// <summary>
        /// In case there are more input samples than the size of the filter, split it in parts.
        /// </summary>
        void ProcessTimeslot(float[] samples, int from, int to) {
            // Move samples and pad present
            for (int i = from; i < to; ++i)
                present[i - from] = new Complex(samples[i]);
            for (int i = to - from; i < present.Length; ++i)
                present[i].Clear();

            // Perform the convolution
            present.InPlaceFFT(cache);
            present.Convolve(filter);
            present.InPlaceIFFT(cache);

            // Append the result to the future
            for (int i = 0; i < present.Length; ++i)
                future[i + delay] += present[i].Real;

            // Write the output and remove those samples from the future
            to -= from;
            for (int i = 0; i < to; ++i)
                samples[from + i] = future[i];
            Array.Copy(future, to, future, 0, future.Length - to);
            Array.Clear(future, future.Length - to, to);
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
        /// Performs the convolution of two real signals. The real result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/>
        /// to match in a length of a power of 2.</remarks>
        public static float[] Convolve(float[] excitation, float[] impulse, FFTCache cache = null) {
            Complex[] result = cache != null?
                ConvolveFourier(excitation, impulse, cache) :
                ConvolveFourier(excitation, impulse);
            result.InPlaceIFFT();
            return Measurements.GetRealPart(result);
        }
    }
}