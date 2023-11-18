namespace Test.Cavern {
    /// <summary>
    /// Common utilities used in testing like assertions.
    /// </summary>
    internal static class TestUtils {
        /// <summary>
        /// Test if the number of zeros in a jagged <paramref name="array"/> match an expected <paramref name="count"/>.
        /// </summary>
        public static void AssertNumberOfZeros(float[][] array, int count) {
            int zeros = 0;
            for (int i = 0; i < array.Length; i++) {
                float[] subarray = array[i];
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