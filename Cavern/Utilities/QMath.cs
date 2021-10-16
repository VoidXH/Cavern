using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    /// <summary>Two plus two is four, minus one, that's three, quick maths.</summary>
    public static class QMath {
        /// <summary>Clamp a double between limits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>Clamp a float between limits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>Clamp an int between limits.</summary>
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

        /// <summary>Unclamped linear interpolation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 from, Vector2 to, float t) => (to - from) * t + from;

        /// <summary>Gets t for linear interpolation for a given value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpInverse(float from, float to, float value) => (value - from) / (to - from);

        /// <summary>Gets t for linear interpolation for a given value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LerpInverse(double from, double to, double value) => (value - from) / (to - from);

        /// <summary>Hack for <see cref="Log2(int)"/> to use in-CPU float conversion as log2 by shifting the exponent.</summary>
        [StructLayout(LayoutKind.Explicit)]
        struct ConverterStruct {
            [FieldOffset(0)] public int asInt;
            [FieldOffset(0)] public float asFloat;
        }

        /// <summary>Compute the base 2 logarithm of a number faster than a generic Log function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int val) {
            ConverterStruct a = new ConverterStruct {
                asFloat = val
            };
            return ((a.asInt >> 23) + 1) & 0x1F;
        }

        /// <summary>Checks if the two numbers have the same sign.</summary>
        /// <remarks>This function does not handle 0, 0 correctly for optimization purposes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SignCompare(float a, float b) => a * b > 0;

        /// <summary>Sum all elements in an array.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(float[] array) {
            float sum = 0;
            for (int i = 0; i < array.Length; ++i)
                sum += array[i];
            return sum;
        }

        /// <summary>Sum absolute values of elements in an array.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(float[] array) {
            float sum = 0;
            for (int i = 0; i < array.Length; ++i)
                sum += Math.Abs(array[i]);
            return sum;
        }
    }
}
