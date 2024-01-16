using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs an optimized convolution.
    /// </summary>
    /// <remarks>This filter is using the overlap and add method using FFTs, with non-thread-safe caches.
    /// For a thread-safe fast convolver, use <see cref="ThreadSafeFastConvolver"/>. This filter is also
    /// only for a single channel, use <see cref="MultichannelConvolver"/> for multichannel signals.</remarks>
    public partial class FastConvolver : Filter, IDisposable {
        /// <summary>
        /// CavernAmp instance of a FastConvolver.
        /// </summary>
        readonly IntPtr native;

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
            if (CavernAmp.Available && CavernAmp.IsMono()) { // CavernAmp only improves performance when the runtime has no SIMD
                native = CavernAmp.FastConvolver_Create(impulse, delay);
                return;
            }
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
            if (native != IntPtr.Zero) {
                CavernAmp.FastConvolver_Process(native, samples, 0, 1);
                return;
            }
            int start = 0;
            while (start < samples.Length) {
                ProcessTimeslot(samples, start, Math.Min(samples.Length, start += filter.Length >> 1));
            }
        }

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            if (native != IntPtr.Zero) {
                CavernAmp.FastConvolver_Process(native, samples, channel, channels);
                return;
            }
            int start = 0,
                end = samples.Length / channels;
            while (start < end) {
                ProcessTimeslot(samples, channel, channels, start, Math.Min(end, start += filter.Length >> 1));
            }
        }

        /// <summary>
        /// Free up the native resources if they were used.
        /// </summary>
        public void Dispose() {
            if (native != IntPtr.Zero) {
                CavernAmp.FastConvolver_Dispose(native);
            }
        }

        /// <summary>
        /// In case there are more input samples than the size of the filter, split it in parts.
        /// </summary>
        /// <param name="samples">Signal that contains the timeslot to process</param>
        /// <param name="from">First sample to process (inclusive)</param>
        /// <param name="to">Last sample to process (exclusive)</param>
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

            ProcessCache(sourceLength + (filter.Length >> 1));

            // Write the output and remove those samples from the future
            to -= from;
            Array.Copy(future, 0, samples, from, to);
            Array.Copy(future, to, future, 0, future.Length - to);
            Array.Clear(future, future.Length - to, to);
        }

        /// <summary>
        /// In case there are more input samples than the size of the filter, split it in parts.
        /// </summary>
        /// <param name="samples">Interlaced signal that contains the timeslot to process</param>
        /// <param name="channel">The channel to process</param>
        /// <param name="channels">Total channels contained in the <paramref name="samples"/></param>
        /// <param name="from">First sample to process (for a single channel, inclusive)</param>
        /// <param name="to">Last sample to process (for a single channel, exclusive)</param>
        unsafe void ProcessTimeslot(float[] samples, int channel, int channels, int from, int to) {
            // Move samples and pad present
            int sourceLength = to - from;
            fixed (float* pSamples = samples)
            fixed (Complex* pPresent = present) {
                float* sample = pSamples + from * channels + channel,
                    lastSample = sample + sourceLength * channels;
                Complex* timeslot = pPresent;
                while (sample != lastSample) {
                    *timeslot++ = new Complex(*sample);
                    sample += channels;
                }
            }
            Array.Clear(present, sourceLength, present.Length - sourceLength);

            ProcessCache(sourceLength + (filter.Length >> 1));

            // Write the output and remove those samples from the future
            fixed (float* pSamples = samples)
            fixed (float* pFuture = future) {
                float* source = pFuture,
                    sample = pSamples + from * channels + channel,
                    lastSample = sample + sourceLength * channels;
                while (sample != lastSample) {
                    *sample = *source++;
                    sample += channels;
                }
            }
            to -= from;
            Array.Copy(future, to, future, 0, future.Length - to);
            Array.Clear(future, future.Length - to, to);
        }

        /// <summary>
        /// When <see cref="present"/> is filled with the source samples, it will be convolved and put into the <see cref="future"/>.
        /// </summary>
        /// <param name="maxResultLength">The maximum number of samples the convolution can result in, which equals
        /// block samples + filter samples.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void ProcessCache(int maxResultLength) {
            // Perform the convolution
            present.InPlaceFFT(cache);
            present.Convolve(filter);
            present.InPlaceIFFT(cache);

            // Append the result to the future
            fixed (Complex* pPresent = present)
            fixed (float* pFuture = future) {
                Complex* source = pPresent;
                float* destination = pFuture + delay,
                    end = destination + maxResultLength;
                while (destination != end) {
                    *destination++ += (*source++).Real;
                }
            }
        }
    }
}