using System;

namespace Cavern.Filters {
    /// <summary>
    /// Performs a Hilbert transform for a 90-degree phase shift.
    /// </summary>
    /// <remarks>This filter is based on the <see cref="FastConvolver"/>.</remarks>
    public class PhaseShifter : FastConvolver {
        /// <summary>
        /// Creates a phase shifter for a given block size.
        /// </summary>
        public PhaseShifter(int blockSize) : base(GenerateFilter(blockSize)) { }

        /// <summary>
        /// Generate the Hilbert transform's impulse response for a given block size.
        /// </summary>
        static float[] GenerateFilter(int blockSize) {
            float[] result = new float[blockSize];
            int half = blockSize / 2;
            for (int i = half--; i < blockSize; i++) {
                result[i] = 1 / ((i - half) * MathF.PI);
            }
            ++half;
            for (int i = 0; i < half; i++) {
                result[i] = 1 / ((-half + i) * MathF.PI);
            }
            return result;
        }
    }
}