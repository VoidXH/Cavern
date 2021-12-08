using System;
using System.Runtime.InteropServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// The format of PEQ filters resulting from CavernAmp's PeakingEqualizer.
    /// </summary>
    internal struct CavernAmpPeakingEQ {
#pragma warning disable 0649 // Set when returning from C++
        public double centerFreq, q, gain;
#pragma warning restore 0649
    }

    /// <summary>
    /// Drastically faster versions of some functions written in C++.
    /// </summary>
    /// <remarks>Use alongside <see cref="CavernAmp"/>!</remarks>
    internal static class CavernQuickEQAmp {
        #region FilterAnalyzer
        /// <summary>
        /// Filter analyzer constructor.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FilterAnalyzer_Create")]
        internal static extern IntPtr FilterAnalyzer_Create(int sampleRate, double maxGain, double minGain,
            double gainPrecision, double startQ, int iterations);

        /// <summary>
        /// Reset a filter with a PeakingEQ.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FilterAnalyzer_AddPEQ")]
        internal static extern void FilterAnalyzer_AddPEQ(IntPtr analyzer, double centerFreq, double q, double gain);

        /// <summary>
        /// Dispose a filter analyzer.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "FilterAnalyzer_Dispose")]
        internal static extern void FilterAnalyzer_Dispose(IntPtr analyzer);
        #endregion

        #region PeakingEqualizer
        /// <summary>
        /// Measure a filter candidate for <see cref="BruteForceQ(float[], int, IntPtr, double, double)"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "BruteForceStep")]
        internal static extern float BruteForceStep(float[] target, int targetLength, float[] changedTarget, IntPtr analyzer);

        /// <summary>
        /// Correct <paramref name="target"/> to the frequency response with the inverse of the found filter.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "BruteForceQ")]
        internal static extern CavernAmpPeakingEQ BruteForceQ(float[] target, int targetLength, IntPtr analyzer, double freq, double gain);

        /// <summary>
        /// Finds a PeakingEQ to correct the worst problem on the input spectrum.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "BruteForceBand")]
        internal static extern CavernAmpPeakingEQ BruteForceBand(float[] target, int targetLength, IntPtr analyzer, int startPos, int stopPos);
        #endregion
    }
}