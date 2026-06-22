using Test.Cavern.Consts;

namespace Test.Cavern;

/// <summary>
/// Common utilities used in testing like assertions.
/// </summary>
internal static class TestUtils {
    /// <summary>
    /// Check if all values of an <paramref name="array"/> are equal to a given <paramref name="value"/>.
    /// </summary>
    public static void AssertAll<T>(T[] array, T value) {
        for (int i = 0; i < array.Length; i++) {
            if (!array[i].Equals(value)) {
                Assert.AreEqual(value, array[i]);
            }
        }
    }

    /// <summary>
    /// Test if the number of zeros in an array <paramref name="list"/> match an expected <paramref name="count"/>.
    /// </summary>
    public static void AssertNumberOfZeros(IList<float[]> list, int count) {
        int zeros = 0;
        for (int i = 0, c = list.Count; i < c; i++) {
            float[] subarray = list[i];
            for (int j = 0; j < subarray.Length; j++) {
                if (subarray[j] == 0) {
                    zeros++;
                }
            }
        }
        Assert.AreEqual(count, zeros);
    }

    /// <summary>
    /// Check if all values in the arrays match until one array's end is reached, and the rest of the values are not set.
    /// </summary>
    public static void AssertSameArrayBeginning<T>(T[] arr1, T[] arr2) {
        int commonUntil = Math.Min(arr1.Length, arr2.Length);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0, end = commonUntil; i < end; i++) {
            if (!comparer.Equals(arr1[i], arr2[i])) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }

        T[] greater = arr1.Length > arr2.Length ? arr1 : arr2;
        for (int i = commonUntil; i < greater.Length; i++) {
            Assert.AreEqual(default, greater[i]);
        }
    }

    /// <summary>
    /// Check if all values in the arrays match until one array's end is reached, and the rest of the values are not set.
    /// </summary>
    public static void AssertSameArrayBeginning(float[] arr1, float[] arr2) => AssertSameArrayBeginning(arr1, arr2, Constants.delta);

    /// <summary>
    /// Check if all values in the arrays match until one array's end is reached, and the rest of the values are not set.
    /// </summary>
    public static void AssertSameArrayBeginning(float[] arr1, float[] arr2, float delta) {
        int commonUntil = Math.Min(arr1.Length, arr2.Length);
        for (int i = 0; i < commonUntil; i++) {
            if (Math.Abs(arr1[i] - arr2[i]) > delta) {
                Assert.AreEqual(arr1[i], arr2[i], delta);
            }
        }

        float[] greater = arr1.Length > arr2.Length ? arr1 : arr2;
        for (int i = commonUntil; i < greater.Length; i++) {
            Assert.AreEqual(0, greater[i], delta);
        }
    }
}
