namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Common utilities used in testing like assertions.
    /// </summary>
    internal static class TestUtils {
        /// <summary>
        /// Test if an <paramref name="array"/> between the given limits is strictly monotonously decreasing,
        /// but allowing an error margin of <paramref name="epsilon"/>.
        /// </summary>
        public static void AssertDecrease(float[] array, int from, int to, float epsilon) {
            for (int i = from + 1; i < to; i++) {
                if (array[i - 1] + epsilon <= array[i]) {
                    Assert.Fail();
                }
            }
        }
    }
}