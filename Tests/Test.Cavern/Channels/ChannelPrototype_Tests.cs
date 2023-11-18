using Cavern.Channels;

namespace Test.Cavern.Channels {
    /// <summary>
    /// Tests the <see cref="ChannelPrototype"/> struct.
    /// </summary>
    [TestClass]
    public class ChannelPrototype_Tests {
        /// <summary>
        /// Tests if the standard layouts have correct channel counts.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void LayoutSizes() {
            for (int i = 1; i <= 16; i++) {
                Assert.AreEqual(i, ChannelPrototype.GetStandardMatrix(i).Length);
                Assert.AreEqual(i, ChannelPrototype.GetIndustryStandardMatrix(i).Length);
            }
        }
    }
}