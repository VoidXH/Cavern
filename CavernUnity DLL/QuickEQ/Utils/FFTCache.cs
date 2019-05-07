using Cavern.Utilities;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Precalculated constants and preallocated recursion arrays for a given FFT size.</summary>
    /// <remarks>Avoid simultaneously calculating two FFTs (since the split arrays are shared), unless you use <see cref="ThreadSafeFFTCache"/>.</remarks>
    public class FFTCache {
        /// <summary>Cosines.</summary>
        internal float[] Cos;
        /// <summary>Sines.</summary>
        internal float[] Sin;
        /// <summary>Preallocated even split arrays. Globally cached in single-threaded applications.</summary>
        internal virtual Complex[][] Even => GlobalEven;
        /// <summary>Preallocated odd split arrays. Globally cached in single-threaded applications.</summary>
        internal virtual Complex[][] Odd => GlobalOdd;
        /// <summary>Preallocated recursion arrays. Shared between all caches, their sizes are 2^i.</summary>
        static readonly Complex[][] GlobalEven = new Complex[30][], GlobalOdd = new Complex[30][];

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

    /// <summary>Thread-safe version of <see cref="FFTCache"/>. Uses its own split cache arrays. Use one instance per thread.</summary>
    public class ThreadSafeFFTCache : FFTCache {
        /// <summary>Preallocated even split arrays.</summary>
        internal override Complex[][] Even { get; } = new Complex[30][];
        /// <summary>Preallocated odd split arrays.</summary>
        internal override Complex[][] Odd { get; } = new Complex[30][];

        /// <summary>Thread-safe <see cref="FFTCache"/> constructor. Does not reference shared split arrays.</summary>
        public ThreadSafeFFTCache(int Size) : base(Size) { }
    }
}