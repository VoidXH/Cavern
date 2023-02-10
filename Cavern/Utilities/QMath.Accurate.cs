using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    public static partial class QMath {
        /// <summary>
        /// Calculate the average of an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AverageAccurate(this double[] array) => SumAccurate(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(this float[] array) => SumAccurate(array, 0, array.Length) / array.Length;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AverageAccurate(this double[] array, int until) => SumAccurate(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array until the selected border element (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(this float[] array, int until) => SumAccurate(array, 0, until) / until;

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AverageAccurate(this double[] array, int from, int to) => SumAccurate(array, from, to) / (to - from);

        /// <summary>
        /// Calculate the average of an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Average(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AverageAccurate(this float[] array, int from, int to) => SumAccurate(array, from, to) / (to - from);

        /// <summary>
        /// Sum all elements in an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumAccurate(this double[] array) => SumAccurate(array, 0, array.Length);

        /// <summary>
        /// Sum all elements in an array without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[])"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(this float[] array) => SumAccurate(array, 0, array.Length);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive) without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumAccurate(this double[] array, int until) => SumAccurate(array, 0, until);

        /// <summary>
        /// Sum the elements in an array until the selected border element (exclusive) without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(this float[] array, int until) => SumAccurate(array, 0, until);

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int, int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumAccurate(this double[] array, int from, int to) {
            double sum = 0f,
                c = 0f;
            for (int i = from; i < to; ++i) {
                double y = array[i] - c;
                double t = sum + y;
                c = (t - sum) - y;
                sum = t;
            }
            return sum;
        }

        /// <summary>
        /// Sum the elements in an array between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive)
        /// without floating point rounding errors.
        /// </summary>
        /// <remarks>This is slower than <see cref="Sum(float[], int, int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SumAccurate(this float[] array, int from, int to) {
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