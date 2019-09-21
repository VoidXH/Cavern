using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Two plus two is four, minus one, that's three.</summary>
    public static class QuickMaths {
        /// <summary>
        /// Clamp a float between limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>
        /// Clamp an int between limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>Unclamped linear interpolation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float from, float to, float t) => (to - from) * t + from;

        /// <summary>Unclamped linear interpolation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double from, double to, double t) => (to - from) * t + from;

        /// <summary>Gets t for linear interpolation for a given value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpInverse(float from, float to, float value) => (value - from) / (to - from);

        /// <summary>Gets t for linear interpolation for a given value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LerpInverse(double from, double to, double value) => (value - from) / (to - from);

        /// <summary>Compute the base 2 logarithm of a number faster than a generic Log function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int value) {
            int log = -1;
            while (value > 255) {
                log += 8;
                value >>= 8;
            }
            while (value != 0) {
                ++log;
                value >>= 1;
            }
            return log;
        }
    }
}
