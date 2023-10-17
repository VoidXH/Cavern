using System;
using System.Numerics;
using System.Threading;

namespace Cavern.Utilities {
    /// <summary>
    /// Precalculated constants and preallocated recursion arrays for a given FFT size.
    /// </summary>
    /// <remarks>Avoid simultaneously calculating two FFTs (since the split arrays are shared),
    /// unless you use <see cref="ThreadSafeFFTCache"/>.
    /// </remarks>
    public class FFTCache : IDisposable {
        /// <summary>
        /// Number of samples in an FFT this cache can maximally handle.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Preallocated even split arrays. Globally cached in single-threaded applications.
        /// </summary>
        internal virtual Complex[][] Even => globalEven;

        /// <summary>
        /// Preallocated odd split arrays. Globally cached in single-threaded applications.
        /// </summary>
        internal virtual Complex[][] Odd => globalOdd;

        /// <summary>
        /// Preallocated recursion arrays. Shared between all caches, their sizes are 2^i.
        /// </summary>
        static readonly Complex[][] globalEven = new Complex[30][], globalOdd = new Complex[30][];

        /// <summary>
        /// Cached cosines for each FFT band at each depth, doubled for SIMD operations.
        /// </summary>
        internal static readonly float[][] cos = new float[30][];

        /// <summary>
        /// Cached sines for each FFT band at each depth, doubled for SIMD operations.
        /// </summary>
        internal static readonly float[][] sin = new float[30][];

        /// <summary>
        /// Alternating 1s and 0s up to the maximum vector length supported by the CLR and the CPU.
        /// </summary>
        internal static Vector<float> evenMask;

        /// <summary>
        /// Alternating 0s and 1s up to the maximum vector length supported by the CLR and the CPU.
        /// </summary>
        internal static Vector<float> oddMask;

        /// <summary>
        /// C++ FFT cache class memory address to be passed to <see cref="CavernAmp"/>.
        /// </summary>
        internal IntPtr Native { get; private set; }

        /// <summary>
        /// Number of existing FFTCache instances of each size.
        /// </summary>
        static readonly int[] refcounts = new int[30];

        /// <summary>
        /// FFT cache constructor.
        /// </summary>
        public FFTCache(int size) {
            Size = size;
            if (CavernAmp.Available && CavernAmp.IsMono()) { // CavernAmp only improves performance when the runtime has no SIMD
                Native = CavernAmp.FFTCache_Create(size);
                return;
            }

            for (int i = 0, c = QMath.Log2(size); i < c; i++) {
                Interlocked.Increment(ref refcounts[i]);
                if (cos[i] != null) {
                    continue;
                }
                int elements = 2 << i;
                float step = -MathF.PI / elements;
                if (cos[i] == null) {
                    float[] thisCos = cos[i] = new float[elements];
                    float[] thisSin = sin[i] = new float[elements];
                    for (int j = 0; j < elements; j += 2) {
                        float rotation = j * step;
                        float cosValue = MathF.Cos(rotation),
                            sinValue = MathF.Sin(rotation);
                        thisCos[j] = cosValue;
                        thisCos[j + 1] = cosValue;
                        thisSin[j] = sinValue;
                        thisSin[j + 1] = sinValue;
                    }
                }
            }

            float[] maskSource = new float[Vector<float>.Count + 1];
            for (int i = 0; i < maskSource.Length; i++) {
                int even = (i & 1) == 0 ? 1 : 0;
                maskSource[i] = even;
            }
            evenMask = new Vector<float>(maskSource);
            oddMask = new Vector<float>(maskSource, 1);

            CreateCacheArrays(size);
        }

        /// <summary>
        /// Free all used resources if there is any.
        /// </summary>
        public void Dispose() {
            if (Native != IntPtr.Zero) {
                CavernAmp.FFTCache_Dispose(Native);
            }
        }

        /// <summary>
        /// Create the arrays where the even-odd splits will be placed.
        /// </summary>
        void CreateCacheArrays(int size) {
            for (int depth = 0, maxDepth = QMath.Log2(size); depth < maxDepth; ++depth) {
                if (Even[depth] == null) {
                    Even[depth] = new Complex[1 << depth];
                    Odd[depth] = new Complex[1 << depth];
                }
            }
        }

        /// <summary>
        /// Free the memory of large cos/sin caches when they're not required anymore.
        /// </summary>
        ~FFTCache() {
            for (int i = 0, c = QMath.Log2(Size); i < c; i++) {
                if (Interlocked.Decrement(ref refcounts[i]) == 0) {
                    cos[i] = null;
                    sin[i] = null;
                }
            }
        }
    }

    /// <summary>
    /// Thread-safe version of <see cref="FFTCache"/>. Uses its own split cache arrays. Use one instance per thread.
    /// </summary>
    /// <remarks>With <see cref="CavernAmp"/>, all <see cref="FFTCache"/>s are thread-safe.</remarks>
    public sealed class ThreadSafeFFTCache : FFTCache {
        /// <summary>
        /// Preallocated even split arrays.
        /// </summary>
        internal override Complex[][] Even { get; } = new Complex[30][];

        /// <summary>
        /// Preallocated odd split arrays.
        /// </summary>
        internal override Complex[][] Odd { get; } = new Complex[30][];

        /// <summary>
        /// Thread-safe <see cref="FFTCache"/> constructor. Does not reference shared split arrays.
        /// </summary>
        public ThreadSafeFFTCache(int size) : base(size) { }
    }
}