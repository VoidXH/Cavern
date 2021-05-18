using System;
using System.Runtime.InteropServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>Drastically faster versions of some functions written in C++.</summary>
    /// <remarks>Use alongside <see cref="CavernAmp"/>!</remarks>
    internal static class CavernQuickEQAmp {
        /// <summary>FFT cache constructor.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Create")]
        internal static extern IntPtr FFTCache_Create(int size);

        /// <summary>Get the creation size of the FFT cache.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Size")]
        internal static extern int FFTCache_Size(IntPtr cache);

        /// <summary>Dispose an FFT cache.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Dispose")]
        internal static extern void FFTCache_Dispose(IntPtr cache);

        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessFFT")]
        internal static extern void ProcessFFT(Complex[] samples, int sampleCount, IntPtr cache, int depth);

        /// <summary>Fourier-transform a signal in 1D. The result is the spectral power.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessFFT1D")]
        internal static extern void ProcessFFT(float[] samples, int sampleCount, IntPtr cache);

        /// <summary>Fast Fourier transform a 2D signal while keeping the source array allocation.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "InPlaceFFT")]
        internal static extern void InPlaceFFT(Complex[] samples, int sampleCount, IntPtr cache);

        /// <summary>Spectrum of a signal's FFT while keeping the source array allocation.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "InPlaceFFT1D")]
        internal static extern void InPlaceFFT(float[] samples, int sampleCount, IntPtr cache);
    }
}