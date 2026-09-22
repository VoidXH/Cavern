using System;
using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    // Native versions of Filters' functions.
    public static partial class CavernAmp {
        /// <summary>
        /// Apply a filter to an array of samples (single channel).
        /// </summary>
        /// <param name="instance">Native filter instance</param>
        /// <param name="samples">Input samples</param>
        internal static unsafe void Filter_Process(IntPtr instance, float[] samples) {
            fixed (float* pSamples = samples) {
                Filter_Process(instance, pSamples, samples.Length);
            }
        }

        /// <summary>
        /// Apply a filter to an array of samples (interleaved channels).
        /// </summary>
        /// <param name="instance">Native filter instance</param>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        internal static unsafe void Filter_Process(IntPtr instance, float[] samples, int channel, int channels) {
            fixed (float* pSamples = samples) {
                Filter_ProcessChannel(instance, pSamples, samples.Length, channel, channels);
            }
        }

        /// <summary>
        /// Create a copy of the filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        internal static extern IntPtr Filter_Clone(IntPtr instance);

        /// <summary>
        /// Dispose the filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        internal static extern void Filter_Dispose(IntPtr instance);

        /// <summary>
        /// Apply a filter to an array of samples (single channel).
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void Filter_Process(IntPtr instance, float* samples, int len);

        /// <summary>
        /// Apply a filter to an array of samples (interleaved channels).
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void Filter_ProcessChannel(IntPtr instance, float* samples, int len, int channel, int channels);

        #region Biquad Simulation
        /// <summary>
        /// Get the transfer function of a biquad filter instance.
        /// </summary>
        /// <param name="instance">Native BiquadFilter instance</param>
        /// <param name="bins">Number of frequency bins (linear spacing from 0 to Nyquist)</param>
        /// <param name="output">Pre-allocated array of Complex, size = bins</param>
        [DllImport("CavernAmp.dll", EntryPoint = "BiquadFilter_GetTransferFunction")]
        public static extern unsafe void BiquadFilter_GetTransferFunction(IntPtr instance, int bins, Complex* output);

        /// <summary>
        /// Get the frequency response (magnitude) of a biquad filter instance.
        /// </summary>
        /// <param name="instance">Native BiquadFilter instance</param>
        /// <param name="bins">Number of frequency bins (linear spacing from 0 to Nyquist)</param>
        /// <param name="output">Pre-allocated array of float, size = bins</param>
        [DllImport("CavernAmp.dll", EntryPoint = "BiquadFilter_GetFrequencyResponse")]
        public static extern unsafe void BiquadFilter_GetFrequencyResponse(IntPtr instance, int bins, float* output);
        #endregion

        #region FastConvolver
        /// <summary>
        /// Constructs an optimized convolution with added delay.
        /// </summary>
        internal static unsafe IntPtr FastConvolver_Create(float[] impulse, int delay) {
            fixed (float* pImpulse = impulse) {
                return FastConvolver_Create(pImpulse, impulse.Length, delay);
            }
        }

        /// <summary>
        /// Returns the actually allocated filter length.
        /// </summary>
        /// <param name="instance">Native FastConvolver instance</param>
        [DllImport("CavernAmp.dll")]
        internal static extern int FastConvolver_GetLength(IntPtr instance);

        /// <summary>
        /// Deconstruct the filter and move it to the preallocated output buffer.
        /// </summary>
        /// <param name="instance">Native FastConvolver instance</param>
        /// <param name="output">Copy the samples to this buffer</param>
        internal static unsafe void FastConvolver_GetFilter(IntPtr instance, float[] output) {
            fixed (float* pOutput = output) {
                FastConvolver_GetFilter(instance, pOutput);
            }
        }

        /// <summary>
        /// Dispose a native FastConvolver <paramref name="instance"/>.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        internal static extern void FastConvolver_Dispose(IntPtr instance);

        /// <summary>
        /// Constructs an optimized convolution with added delay.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe IntPtr FastConvolver_Create([In] float* impulse, int len, int delay);

        /// <summary>
        /// Deconstruct the filter and move it to the preallocated output buffer.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void FastConvolver_GetFilter(IntPtr instance, [Out] float* output);
        #endregion
    }
}
