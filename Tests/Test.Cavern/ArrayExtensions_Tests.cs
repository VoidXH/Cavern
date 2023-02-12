using Cavern.Utilities;

namespace Test.Cavern {
    [TestClass]
    public class ArrayExtensions_Tests {
        [TestMethod, Timeout(1000)]
        public void Nearest() {
            float[] testArray = { 1, 2, 3, 4 };
            Assert.AreEqual(testArray.Nearest(1.99f), 2);
            Assert.AreEqual(testArray.Nearest(3.01f), 3);
        }
    }
}