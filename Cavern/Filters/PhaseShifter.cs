using System;

namespace Cavern.Filters {
    /// <summary>
    /// Performs a Hilbert transform for a 90 or -90-degree phase shift.
    /// </summary>
    /// <remarks>This filter is based on the <see cref="FastConvolver"/>.</remarks>
    public class PhaseShifter : FastConvolver {
        /// <summary>
        /// Creates a 90-degree phase shifter for a given block size.
        /// </summary>
        /// <param name="blockSize">Length of the filter</param>
        public PhaseShifter(int blockSize) : this(blockSize, true) { }

        /// <summary>
        /// Creates a phase shift in a given direction.
        /// </summary>
        /// <param name="blockSize">Length of the filter</param>
        /// <param name="forward">True for a 90-degree phase shift, false for a -90-degree phase shift</param>
        public PhaseShifter(int blockSize, bool forward) : base(GenerateFilter(blockSize, forward)) { }

        /// <summary>
        /// Generate the Hilbert transform's impulse response for a given block size: 1/x with even elements being 0.
        /// </summary>
        public static float[] GenerateFilter(int blockSize, bool forward) {
            float[] result = new float[blockSize];
            int center = blockSize / 2;
            float dir = forward ? 1 : -1;
            float idealMul = dir / MathF.PI;
            float windowMul = 2 * MathF.PI / blockSize;

            for (int i = 0; i < blockSize; i++) {
                int t = i - center;
                if (t % 2 == 0) {
                    result[i] = 0.0f;
                } else {
                    float ideal = idealMul / t;
                    float window = .5f * (MathF.Cos(windowMul * t) + 1);
                    result[i] = ideal * window;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public override object Clone() => new PhaseShifter(Length);
    }
}