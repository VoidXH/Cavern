using Cavern.Utilities;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Precalculated constants and preallocated recursion arrays for a given FFT size.</summary>
    public class FFTCache {
        /// <summary>Cosines.</summary>
        internal float[] Cos;
        /// <summary>Sines.</summary>
        internal float[] Sin;
        /// <summary>Preallocated recursion arrays. Shared between all caches, their sizes are 2^i.</summary>
        internal static readonly Complex[][] Even = new Complex[30][], Odd = new Complex[30][];

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
            int Log = CavernUtilities.Log2(Size);
            for (int Depth = 0; Depth < Log; ++Depth) {
                int DepthSize = 1 << Depth;
                Even[Depth] = new Complex[DepthSize];
                Odd[Depth] = new Complex[DepthSize];
            }
        }
    }
}