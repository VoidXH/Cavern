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
            Assert.AreEqual(2, testArray.Nearest(1.99f));
            Assert.AreEqual(3, testArray.Nearest(3.01f));
        }
    }
}