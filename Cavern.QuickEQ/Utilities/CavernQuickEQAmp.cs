using Cavern.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>Drastically faster versions of some functions written in C++.</summary>
    /// <remarks>Use alongside <see cref="Cavern.Utilities.CavernAmp"/>!</remarks>
    public static class CavernQuickEQAmp {
        /// <summary>FFT cache constructor.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Create")]
        public static extern IntPtr FFTCache_Create(int size);

        /// <summary>Get the creation size of the FFT cache.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Size")]
        public static extern int FFTCache_Size(IntPtr cache);

        /// <summary>Dispose an FFT cache.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FFTCache_Dispose")]
        public static extern void FFTCache_Dispose(IntPtr cache);

        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "ProcessFFT")]
        public static extern void ProcessFFT(Complex[] samples, int sampleCount, IntPtr cache, int depth);
    }
}