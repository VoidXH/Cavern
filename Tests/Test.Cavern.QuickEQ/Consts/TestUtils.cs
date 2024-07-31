namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Common utilities used in testing like assertions.
    /// </summary>
    internal static class TestUtils {
        /// <summary>
        /// Test if an <paramref name="array"/> between the given limits is strictly monotonously decreasing,
        /// but allowing an error margin of <paramref name="delta"/>.
        /// </summary>
        public static void AssertDecrease(float[] array, int from, int to, float delta) {
            for (int i = from + 1; i < to; i++) {
                if (array[i - 1] + delta <= array[i]) {
                    Assert.Fail();
                }
            }
        }

        /// <summary>
        /// Test if all values of both arrays are in the expected margin of error (<paramref name="delta"/>) from each other.
        /// </summary>
        public static void AssertArrayEquals(float[] expected, float[] actual, float delta) {
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i], actual[i], delta);
            }
        }
    }
}