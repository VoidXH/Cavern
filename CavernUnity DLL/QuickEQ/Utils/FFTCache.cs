using Cavern.Utilities;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Precalculated constants and preallocated recursion arrays for a given FFT size.</summary>
    public class FFTCache {
        /// <summary>Cosines.</summary>
        internal float[] Cos;
        /// <summary>Sines.</summary>
        internal float[] Sin;
        /// <summary>Preallocated recursion arrays.</summary>
        internal Complex[][] Even, Odd;

        /// <summary>FFT cache constructor.</summary>
        public FFTCache(int Size) {
            int HalfSize = Size / 2;
            float Step = -2 * Mathf.PI / Size;
            Cos = new float[HalfSize];
            Sin = new float[HalfSize];
            for (int i = 0; i < HalfSize; ++i) {
                float Rotation = i * Step;
                Cos[i] = Mathf.Cos(Rotation);
                Sin[i] = Mathf.Sin(Rotation);
            }
            Even = new Complex[31][];
            Odd = new Complex[31][];
            int DepthLevel = 0;
            while (Size != 0) {
                Even[DepthLevel] = new Complex[Size >>= 1];
                Odd[DepthLevel++] = new Complex[Size];
            }
        }
    }
}