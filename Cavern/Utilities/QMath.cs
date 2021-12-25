using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Two plus two is four, minus one, that's three, quick maths.
    /// </summary>
    public static class QMath {
        /// <summary>
        /// Hack for <see cref="Log2(int)"/> to use in-CPU float conversion as log2 by shifting the exponent.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct ConverterStruct {
            /// <summary>
            /// Get the contained 4 bytes as an integer.
            /// </summary>
            [FieldOffset(0)] public int asInt;

            /// <summary>
            /// Get the contained 4 bytes as a float.
            /// </summary>
            [FieldOffset(0)] public float asFloat;
        }

        /// <summary>
        /// Calculate the average of an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(float[] array) => Sum(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(float[] array, int until) => Sum(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(float[] array, int from, int to) => Sum(array, from, to) / (to - from);

        /// <summary>
        /// Calculate the average of an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(float[] array) => SumAccurate(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive) without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(float[] array, int until) => SumAccurate(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(float[] array, int from, int to) => SumAccurate(array, from, to) / (to - from);

        /// <summary>
        /// Round up the number in base 2.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Base2Ceil(int val) {
            ConverterStruct a = new ConverterStruct {
                asFloat = val
            };
            int result = 1 << (((a.asInt >> 23) + 1) & 0x1F);
            if (result != val)
                return result << 1;
            return result;
        }

        /// <summary>
        /// Clamp a double between limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

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

        /// <summary>
        /// Convert voltage gain to decibels.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GainToDb(float gain) => 20 * (float)Math.Log10(gain);

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float from, float to, float t) => (to - from) * t + from;

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double from, double to, double t) => (to - from) * t + from;

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 from, Vector2 to, float t) => (to - from) * t + from;

        /// <summary>
        /// Gets t for linear interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpInverse(float from, float to, float value) => (value - from) / (to - from);

        /// <summary>
        /// Gets t for linear interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LerpInverse(double from, double to, double value) => (value - from) / (to - from);

        /// <summary>
        /// Compute the base 2 logarithm of a number faster than a generic Log function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int val) {
            ConverterStruct a = new ConverterStruct {
                asFloat = val
            };
            return ((a.asInt >> 23) + 1) & 0x1F;
        }

        /// <summary>
        /// Checks if the two numbers have the same sign.
        /// </summary>
        /// <remarks>This function does not handle 0, 0 correctly for optimization purposes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SignCompare(float a, float b) => a * b > 0;

        /// <summary>
        /// Sum all elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(float[] array) => Sum(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(float[] array, int until) => Sum(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(float[] array, int from, int to) {
            float sum = 0;
            for (int i = from; i < to; ++i)
                sum += array[i];
            return sum;
        }

        /// <summary>
        /// Sum absolute values of elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(float[] array) => SumAbs(array, 0, array.Length);

        /// <summary>
        /// Sum absolute values of elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(float[] array, int until) => SumAbs(array, 0, until);

        /// <summary>
        /// Sum absolute values of elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(float[] array, int from, int to) {
            float sum = 0;
            for (int i = from; i < to; ++i)
                sum += Math.Abs(array[i]);
            return sum;
        }

        /// <summary>
        /// Sum all elements in an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(float[] array) => SumAccurate(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive) without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(float[] array, int until) => SumAccurate(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int, int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(float[] array, int from, int to) {
            float sum = 0f,
                c = 0f;
            for (int i = from; i < to; ++i) {
                float y = array[i] - c;
                float t = sum + y;
                c = (t - sum) - y;
                sum = t;
            }
            return sum;
        }
    }
}