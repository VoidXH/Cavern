namespace Test.Cavern {
    /// <summary>
    /// Common utilities used in testing like assertions.
    /// </summary>
    internal static class TestUtils {
        /// <summary>
        /// Check if all values of an <paramref name="array"/> are equal to a given <paramref name="value"/>.
        /// </summary>
        public static void AssertAll<T>(T[] array, T value) {
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(value, array[i]);
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
    }
}