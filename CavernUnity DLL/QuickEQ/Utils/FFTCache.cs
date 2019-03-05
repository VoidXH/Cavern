using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Precalculated constants for a given FFT size.</summary>
    public class FFTCache {
        /// <summary>Cosines.</summary>
        public float[] Cos;
        /// <summary>Sines.</summary>
        public float[] Sin;

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
        }
    }
}