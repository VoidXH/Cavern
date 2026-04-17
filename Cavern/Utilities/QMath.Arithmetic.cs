using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    partial class QMath {
        /// <summary>
        /// Calculate the average of an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Average(this double[] array) => Sum(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Average(this double[] array, int until) => Sum(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Average(this double[] array, int from, int to) => Sum(array, from, to) / (to - from);

        /// <summary>
        /// Calculate the average of an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(this float[] array) => Sum(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(this float[] array, int until) => Sum(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(this float[] array, int from, int to) => Sum(array, from, to) / (to - from);

        /// <summary>
        /// Sum all elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(this double[] array) => Sum(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(this double[] array, int until) => Sum(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(this double[] array, int from, int to) {
            double sum = 0;
            for (int i = from; i < to; ++i) {
                sum += array[i];
            }
            return sum;
        }

        /// <summary>
        /// Sum all elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(this float[] array) => Sum(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(this float[] array, int until) => Sum(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(this float[] array, int from, int to) {
            float sum = 0;
            for (int i = from; i < to; ++i) {
                sum += array[i];
            }
            return sum;
        }

        /// <summary>
        /// Sum all elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(this int[] array) => Sum(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(this int[] array, int until) => Sum(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(this int[] array, int from, int to) {
            int sum = 0;
            for (int i = from; i < to; ++i) {
                sum += array[i];
            }
            return sum;
        }

        /// <summary>
        /// Sum the elements in a list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(this IReadOnlyList<double> list) {
            double sum = 0;
            for (int i = 0, to = list.Count; i < to; ++i) {
                sum += list[i];
            }
            return sum;
        }

        /// <summary>
        /// Sum absolute values of elements in an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(this float[] array) => SumAbs(array, 0, array.Length);

        /// <summary>
        /// Sum absolute values of elements in an array until the selected border element (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(this float[] array, int until) => SumAbs(array, 0, until);

        /// <summary>
        /// Sum absolute values of elements in an array between <paramref name="from"/> (inclusive)
        /// and <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAbs(this float[] array, int from, int to) {
            float sum = 0;
            for (int i = from; i < to; ++i) {
                sum += Math.Abs(array[i]);
            }
            return sum;
        }
    }
}
