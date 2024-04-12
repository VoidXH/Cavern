using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Functions that generate test data.
    /// </summary>
    public static class Generators {
        /// <summary>
        /// Generates a one period sine wave.
        /// </summary>
        public static float[] Sine(int length) {
            float[] result = new float[length];
            for (int i = 0; i < length; i++) {
                result[i] = MathF.Cos(2 * MathF.PI * i / length);
            }
            return result;
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
    }
}