using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Shorthands for common checks.
    /// </summary>
    public static class Checks {
        /// <summary>
        /// Checks if no nested array is null in addition to the outer <paramref name="array"/>.
        /// </summary>
        public static void AssertElementsNotNull<T>(this T[][] array) where T : struct {
            if (array == null) {
                ThrowArgumentNull(nameof(array));
            }

            for (int i = 0; i < array.Length; i++) {
                if (array[i] == null) {
                    ThrowNestedNull(nameof(array), i);
                }
            }
        }

        /// <summary>
        /// Checks if an <paramref name="array"/> contains a value at a given <paramref name="index"/>, and throws <see cref="IndexOutOfRangeException"/> if not.
        /// </summary>
        /// <remarks>Does not check if the <paramref name="array"/> is null.</remarks>
        public static void AssertInBounds<T>(this T[] array, string name, int index) {
            if (index < 0 || index >= array.Length) {
                throw new IndexOutOfRangeException($"{name}[{index}] is out of the valid range of 0 - {array.Length}.");
            }
        }

        /// <summary>
        /// If the <paramref name="value"/> is negative, throw an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        public static void ThrowIfNegative(this int value, string name) {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException($"{name} is out of the valid range of 0 - {int.MaxValue}");
            }
        }

        /// <summary>
        /// Throw an <see cref="ArgumentNullException"/> if the <paramref name="item"/> is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T item, string name) where T : class {
            if (item == null) {
                ThrowArgumentNull(name);
            }
        }

        /// <summary>
        /// Format an <see cref="ArgumentNullException"/> with an argument <paramref name="name"/>.
        /// </summary>
        static void ThrowArgumentNull(string name) => throw new ArgumentNullException($"{name} can't be null.");

        /// <summary>
        /// Format a <see cref="NullReferenceException"/> for an array element with its <paramref name="name"/> and <paramref name="index"/> for better debugging.
        /// </summary>
        static void ThrowNestedNull(string name, int index) => throw new NullReferenceException($"{name}[{index}] can't be null.");
    }
}
