using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Precalculated constants and preallocated recursion arrays for a given FFT size.
    /// </summary>
    /// <remarks>Avoid simultaneously calculating two FFTs (since the split arrays are shared), unless you use <see cref="ThreadSafeFFTCache"/>.
    /// </remarks>
    public class FFTCache : IDisposable {
        /// <summary>
        /// Cached cosines for each FFT band.
        /// </summary>
        internal float[] cos;

        /// <summary>
        /// Cached sines for each FFT band.
        /// </summary>
        internal float[] sin;

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
        /// C++ FFT cache class memory address to be passed to <see cref="CavernAmp"/>.
        /// </summary>
        internal IntPtr Native { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// FFT cache constructor.
        /// </summary>
        public FFTCache(int size) {
            if (CavernAmp.Available) {
                Native = CavernAmp.FFTCache_Create(size);
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
            for (int depth = 0, maxDepth = QMath.Log2(size); depth < maxDepth; ++depth) {
                if (Even[depth] == null) {
                    Even[depth] = new Complex[1 << depth];
                    Odd[depth] = new Complex[1 << depth];
                }
            }
        }

        /// <summary>
        /// Free all used resources if there is any.
        /// </summary>
        public void Dispose() {
            if (Native.ToInt64() != 0)
                CavernAmp.FFTCache_Dispose(Native);
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