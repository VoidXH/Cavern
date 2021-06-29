using System;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Precalculated constants and preallocated recursion arrays for a given FFT size.</summary>
    public class FFTCache : IDisposable {
        /// <summary>Cosines.</summary>
        internal float[] cos;
        /// <summary>Sines.</summary>
        internal float[] sin;
        /// <summary>Preallocated even split array.</summary>
        internal Complex[] even;
        /// <summary>Preallocated odd split array.</summary>
        internal Complex[] odd;
        /// <summary>Preallocated worker array.</summary>
        internal Complex[] temp;

        /// <summary>C++ FFT cache class memory address to be passed to <see cref="CavernQuickEQAmp"/>.</summary>
        internal IntPtr Native { get; private set; } = new IntPtr(0);

        /// <summary>FFT cache constructor.</summary>
        public FFTCache(int size) {
            if (CavernAmp.Available) {
                Native = CavernQuickEQAmp.FFTCache_Create(size);
                return;
            }

            int halfSize = size / 2;
            double step = -2 * Math.PI / size;
            cos = new float[halfSize];
            sin = new float[halfSize];
            for (int i = 0; i < halfSize; ++i) {
                double rotation = i * step;
                cos[i] = (float)Math.Cos(rotation);
                sin[i] = (float)Math.Sin(rotation);
            }
            even = new Complex[halfSize];
            odd = new Complex[halfSize];
            temp = new Complex[size];
        }

        /// <summary>Free all used resources if there is any.</summary>
        public void Dispose() {
            if (Native.ToInt64() != 0)
                CavernQuickEQAmp.FFTCache_Dispose(Native);
        }
    }
}