using System;
using System.Runtime.InteropServices;

namespace Cavern.Filters {
    /// <summary>
    /// Wrapper for CavernAmp's implementation of <see cref="Gain"/>.
    /// </summary>
    public partial class GainAmp {
        /// <summary>
        /// Constructs a Gain filter with the specified gain in decibels.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr Gain_Create(double db);

        /// <summary>
        /// Returns the gain in decibels.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern double Gain_GetGainValue(IntPtr instance);

        /// <summary>
        /// Sets the gain in decibels.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void Gain_SetGainValue(IntPtr instance, double db);

        /// <summary>
        /// Returns whether the phase is inverted.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern bool Gain_GetInvert(IntPtr instance);

        /// <summary>
        /// Sets whether the phase is inverted.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void Gain_SetInvert(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool invert);
    }
}
