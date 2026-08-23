using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Functions that generate test data.
    /// </summary>
    public static class Generators {
        /// <summary>
        /// Generates a Dirac-delta in time domain.
        /// </summary>
        public static float[] DiracDelta(int length) {
            float [] result = new float[length];
            result[0] = 1;
            return result;
        }

        /// <summary>
        /// A single-sample mono signal placed at <paramref name="offset"/> within one update frame, at the default update rate.
        /// </summary>
        public static float[] DiracDeltaOffset(int offset) {
            float[] pulse = new float[240];
            pulse[offset] = 1;
            return pulse;
        }

        /// <summary>
        /// Generates a Dirac-delta in Fourier-space (constant 1).
        /// </summary>
        public static Complex[] DiracFourier(int length) {
            Complex[] result = new Complex[length];
            for (int i = 0; i < length; i++) {
                result[i].Real = 1;
            }
            return result;
        }

        /// <summary>
        /// Generate a <paramref name="count"/> of random values.
        /// </summary>
        public static float[] Random(int count) {
            float[] result = new float[count];
            Random random = new();
            for (int i = 0; i < result.Length; i++) {
                result[i] = random.NextSingle();
            }
            return result;
        }
    }
}
