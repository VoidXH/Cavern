using Cavern.Utilities;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="MovingAverage"/> class.
    /// </summary>
    [TestClass]
    public class MovingAverage_Tests {
        /// <summary>
        /// Tests if <see cref="MovingAverage"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void MovingAverage() {
            MovingAverage average = new MovingAverage(3);
            float[] first = { 1, 0, 4 };
            average.AddFrame(first);
            Equals(average.Average, first);
            average.AddFrame([2, 0, 4]);
            average.AddFrame([3, 3, 4]);
            Equals(average.Average, new float[] { 2, 1, 4 });
        }
    }
}