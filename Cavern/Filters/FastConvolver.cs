﻿using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs an optimized convolution.
    /// </summary>
    /// <remarks>This filter is using the overlap and add method using FFTs, with non-thread-safe caches.
    /// For a thread-safe fast convolver, use <see cref="ThreadSafeFastConvolver"/>.</remarks>
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
        /// Constructs an optimized convolution with no delay.
        /// </summary>
        public FastConvolver(float[] impulse) : this(impulse, 0) { }

        /// <summary>
        /// Constructs an optimized convolution with added delay.
        /// </summary>
        public FastConvolver(float[] impulse, int delay) {
            int fftSize = 2 << QMath.Log2Ceil(impulse.Length); // Zero padding for the falloff to have space
            cache = CreateCache(fftSize);
            filter = new Complex[fftSize];
            for (int sample = 0; sample < impulse.Length; sample++) {
                filter[sample].Real = impulse[sample];
            }
            filter.InPlaceFFT(cache);
            present = new Complex[fftSize];
            future = new float[fftSize + delay];
            this.delay = delay;
        }

        /// <summary>
        /// Create the FFT cache used for accelerating the convolution in Fourier-space.
        /// </summary>
        public virtual FFTCache CreateCache(int fftSize) => new FFTCache(fftSize);

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            int start = 0;
            while (start < samples.Length) {
                ProcessTimeslot(samples, start, Math.Min(samples.Length, start += filter.Length >> 1));
            }
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
        ///  The <see cref="FFTCache"/> will be created temporarily and performance will suffer.
        /// </summary>
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
        /// In case there are more input samples than the size of the filter, split it in parts.
        /// </summary>
        unsafe void ProcessTimeslot(float[] samples, int from, int to) {
            // Move samples and pad present
            int sourceLength = to - from;
            fixed (float* pSamples = samples)
            fixed (Complex* pPresent = present) {
                float* sample = pSamples + from,
                    lastSample = sample + sourceLength;
                Complex* timeslot = pPresent;
                while (sample != lastSample) {
                    *timeslot++ = new Complex(*sample++);
                }
            }
            Array.Clear(present, sourceLength, present.Length - sourceLength);

            // Perform the convolution
            present.InPlaceFFT(cache);
            present.Convolve(filter);
            present.InPlaceIFFT(cache);

            // Append the result to the future
            fixed (Complex* pPresent = present)
            fixed (float* pFuture = future) {
                Complex* source = pPresent,
                    end = source + present.Length;
                float* destination = pFuture + delay;
                while (source != end) {
                    *destination++ += (*source++).Real;
                }
            }

            // Write the output and remove those samples from the future
            to -= from;
            Array.Copy(future, 0, samples, from, to);
            Array.Copy(future, to, future, 0, future.Length - to);
            Array.Clear(future, future.Length - to, to);
        }
    }
}