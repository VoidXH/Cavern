using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Drastically faster versions of some functions written in C++.
    /// </summary>
    public static partial class CavernAmp {
        /// <summary>
        /// Is the CavernAmp DLL present and the platform is correct?
        /// </summary>
        public static bool Available {
            get {
                if (tested) {
                    return available;
                }

                if (bypass) {
                    return available = false;
                }

                try {
                    // Available when CavernAmp DLL can be called and AVX is supported
                    available = IsAvailable() && (GetEnabledXStateFeatures() & 4) != 0;
                } catch {
                    available = false;
                }
                tested = true;
                return available;
            }
        }

        /// <summary>
        /// Force disable CavernAmp for performance benchmarks.
        /// </summary>
        public static bool Bypass {
            get => bypass;
            set {
                bypass = value;
                if (available) {
                    available = false;
                }
                if (!bypass) {
                    tested = false;
                }
            }
        }

        /// <summary>
        /// Is the CavernAmp DLL present and the platform is correct?
        /// </summary>
        static bool available;

        /// <summary>
        /// Force disable CavernAmp for performance benchmarks.
        /// </summary>
        static bool bypass;

        /// <summary>
        /// True if CavernAmp DLL was checked if <see cref="available"/>.
        /// </summary>
        static bool tested;

        /// <summary>
        /// The running CLR is Mono.
        /// </summary>
        static bool? mono;

        /// <summary>
        /// The running CLR is Mono, which limits optimization possibilities and for example,
        /// Vectors run much slower, they should not be used.
        /// </summary>
        public static bool IsMono() => mono ??= Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Gets supported CPU instruction sets.
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern long GetEnabledXStateFeatures();

        /// <summary>
        /// When the DLL is present near the executable and the platform matches, this returns true.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "IsAvailable")]
        static extern bool IsAvailable();

        #region Measurements
        /// <summary>
        /// FFT cache constructor.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Create")]
        internal static extern IntPtr FFTCache_Create(int size);

        /// <summary>
        /// Get the creation size of the FFT cache.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Size")]
        internal static extern int FFTCache_Size(IntPtr cache);

        /// <summary>
        /// Dispose an FFT cache.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Dispose")]
        internal static extern void FFTCache_Dispose(IntPtr cache);

        /// <summary>
        /// Actual FFT processing, somewhat in-place.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ProcessFFT(Complex[] samples, IntPtr cache, int depth) {
            fixed (Complex* pSamples = samples) {
                ProcessFFT(pSamples, samples.Length, cache, depth);
            }
        }

        /// <summary>
        /// Fourier-transform a signal in 1D. The result is the spectral power.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ProcessFFT(float[] samples, IntPtr cache) {
            fixed (float* pSamples = samples) {
                ProcessFFT(pSamples, samples.Length, cache);
            }
        }

        /// <summary>
        /// Fast Fourier transform a 2D signal while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InPlaceFFT(Complex[] samples, FFTCache cache = null) {
            fixed (Complex* pSamples = samples) {
                InPlaceFFT(pSamples, samples.Length, cache == null ? IntPtr.Zero : cache.Native);
            }
        }

        /// <summary>
        /// Spectrum of a signal's FFT while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InPlaceFFT(float[] samples, FFTCache cache = null) {
            fixed (float* pSamples = samples) {
                InPlaceFFT(pSamples, samples.Length, cache == null ? IntPtr.Zero : cache.Native);
            }
        }

        /// <summary>
        /// Outputs IFFT(X) * N.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ProcessIFFT(Complex[] samples, IntPtr cache, int depth) {
            fixed (Complex* pSamples = samples) {
                ProcessIFFT(pSamples, samples.Length, cache, depth);
            }
        }

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InPlaceIFFT(Complex[] samples, FFTCache cache = null) {
            fixed (Complex* pSamples = samples) {
                InPlaceIFFT(pSamples, samples.Length, cache == null ? IntPtr.Zero : cache.Native);
            }
        }

        /// <summary>
        /// Actual FFT processing, somewhat in-place.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessFFT")]
        static extern unsafe void ProcessFFT(Complex* samples, int sampleCount, IntPtr cache, int depth);

        /// <summary>
        /// Fourier-transform a signal in 1D. The result is the spectral power.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessFFT1D")]
        static extern unsafe void ProcessFFT(float* samples, int sampleCount, IntPtr cache);

        /// <summary>
        /// Fast Fourier transform a 2D signal while keeping the source array allocation.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "InPlaceFFT")]
        static extern unsafe void InPlaceFFT(Complex* samples, int sampleCount, IntPtr cache);

        /// <summary>
        /// Spectrum of a signal's FFT while keeping the source array allocation.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "InPlaceFFT1D")]
        static extern unsafe void InPlaceFFT(float* samples, int sampleCount, IntPtr cache);

        /// <summary>
        /// Outputs IFFT(X) * N.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessIFFT")]
        static extern unsafe void ProcessIFFT(Complex* samples, int sampleCount, IntPtr cache, int depth);

        /// <summary>
        /// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "InPlaceIFFT")]
        static extern unsafe void InPlaceIFFT(Complex* samples, int sampleCount, IntPtr cache);
        #endregion
    }
}