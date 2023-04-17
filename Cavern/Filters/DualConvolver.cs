using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs two optimized convolutions for the cost of one.
    /// </summary>
    /// <remarks>This filter is using the overlap and add method using FFTs.</remarks>
    public class DualConvolver {
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
        readonly Complex[] future;

        /// <summary>
        /// FFT optimization.
        /// </summary>
        readonly FFTCache cache;

        /// <summary>
        /// Delay applied with the real dimension of the convolution.
        /// </summary>
        readonly int delay1;

        /// <summary>
        /// Delay applied with the imaginary dimension of the convolution.
        /// </summary>
        readonly int delay2;

        /// <summary>
        /// Constructs an optimized convolution with no delay.
        /// </summary>
        public DualConvolver(float[] impulse1, float[] impulse2) : this(impulse1, impulse2, 0, 0) { }

        /// <summary>
        /// Constructs an optimized convolution with additional delays.
        /// </summary>
        public DualConvolver(float[] impulse1, float[] impulse2, int delay1, int delay2) {
            int impulseLength = Math.Max(impulse1.Length, impulse2.Length);
            int fftSize = 2 << QMath.Log2Ceil(impulseLength); // Zero padding for the falloff to have space
            cache = new ThreadSafeFFTCache(fftSize);
            filter = new Complex[fftSize];
            for (int sample = 0; sample < impulse1.Length; sample++) {
                filter[sample].Real = impulse1[sample];
            }
            for (int sample = 0; sample < impulse2.Length; sample++) {
                filter[sample].Imaginary = impulse2[sample];
            }
            filter.InPlaceFFT(cache);
            present = new Complex[fftSize];
            future = new Complex[fftSize + Math.Max(delay1, delay2)];
            this.delay1 = delay1;
            this.delay2 = delay2;
        }

        /// <summary>
        /// Apply convolution on both arrays of samples. One filter should be applied to only two continuous streams of samples.
        /// </summary>
        /// <param name="samplesInOut">The samples to be processed, this will be convolved with the first impulse</param>
        /// <param name="samplesOut"><paramref name="samplesInOut"/> convolved with the second impulse</param>
        /// <remarks>The length of both arrays must match.</remarks>
        public void Process(float[] samplesInOut, float[] samplesOut) {
            int start = 0;
            while (start < samplesOut.Length) {
                ProcessTimeslot(samplesInOut, samplesOut, start, Math.Min(samplesOut.Length, start += filter.Length >> 1));
            }
        }

        /// <summary>
        /// In case there are more input samples than the size of the filter, split it in parts.
        /// </summary>
        void ProcessTimeslot(float[] samplesInOut, float[] samplesOut, int from, int to) {
            // Move samples and pad present
            for (int i = from; i < to; i++) {
                present[i - from] = new Complex(samplesInOut[i], 0);
            }
            for (int i = to - from; i < present.Length; i++) {
                present[i].Clear();
            }

            // Perform the convolution
            present.InPlaceFFT(cache);
            present.Convolve(filter);
            present.InPlaceIFFT(cache);

            // Append the result to the future
            for (int i = 0; i < present.Length; i++) {
                future[i + delay1].Real += present[i].Real;
                future[i + delay2].Imaginary += present[i].Imaginary;
            }

            // Write the output and remove those samples from the future
            to -= from;
            for (int i = 0; i < to; i++) {
                samplesInOut[from + i] = future[i].Real;
                samplesOut[from + i] = future[i].Imaginary;
            }
            Array.Copy(future, to, future, 0, future.Length - to);
            Array.Clear(future, future.Length - to, to);
        }
    }
}