using System;
using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    // Native versions of Filters' functions.
    public static partial class CavernAmp {
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
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="instance">Native FastConvolver instance</param>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        internal static unsafe void FastConvolver_Process(IntPtr instance, float[] samples, int channel, int channels) {
            fixed (float* pSamples = samples) {
                FastConvolver_Process(instance, pSamples, samples.Length, channel, channels);
            }
        }

        /// <summary>
        /// Dispose a native FastConvolver <paramref name="instance"/>.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        internal static extern unsafe void FastConvolver_Dispose(IntPtr instance);

        /// <summary>
        /// Constructs an optimized convolution with added delay.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe IntPtr FastConvolver_Create(float* impulse, int len, int delay);

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void FastConvolver_Process(IntPtr instance, float* samples, int len, int channel, int channels);
        #endregion
    }
}