using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="ArrayExtensions"/> class.
    /// </summary>
    [TestClass]
    public class ArrayExtensions_Tests {
        /// <summary>
        /// Tests the <see cref="ArrayExtensions.Nearest(float[], float)"/> method.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Nearest() {
            float[] testArray = { 1, 2, 3, 4 };
            Assert.AreEqual(testArray.Nearest(1.99f), 2);
            Assert.AreEqual(testArray.Nearest(3.01f), 3);
        }
    }
}