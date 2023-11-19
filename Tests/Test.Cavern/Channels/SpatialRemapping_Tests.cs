using Cavern;
using Cavern.Channels;

namespace Test.Cavern.Channels {
    /// <summary>
    /// Tests the <see cref="SpatialRemapping"/> functions.
    /// </summary>
    [TestClass]
    public class SpatialRemapping_Tests {
        /// <summary>
        /// Tests if remapping the alternative 5.1 is done correctly to average 5.1 placement.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Remap5Point1() {
            Channel[] content = ChannelPrototype.ToLayoutAlternative(ChannelPrototype.ref510),
                playback = ChannelPrototype.ToLayout(ChannelPrototype.ref510);
            float[][] matrix = SpatialRemapping.GetMatrix(content, playback);
            Assert.AreEqual(1, matrix[0][0]); // FL
            Assert.AreEqual(1, matrix[1][1]); // FR
            Assert.AreEqual(1, matrix[2][2]); // C
            Assert.AreEqual(1, matrix[3][3]); // LFE
            Assert.AreEqual(.570968032f, matrix[0][4]); // SL front mix
            Assert.AreEqual(.570968032f, matrix[1][5]); // SR front mix
            Assert.AreEqual(.820972264f, matrix[4][4]); // SL side mix
            Assert.AreEqual(.820972264f, matrix[5][5]); // SR side mix
            TestUtils.AssertNumberOfZeros(matrix, 28);
        }

        /// <summary>
        /// Tests if remapping 7.1 is done correctly to 5.1.2 and converted to the valid Equalizer APO line.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Remap7Point1To512APO() {
            const string expected = "Copy: L=L R=R C=C SUB=SUB RL=0.92387956*RL+0.3826834*RR+SL RR=0.38268337*RL+0.92387956*RR+SR SL=0 SR=0";
            string result = SpatialRemapping.ToEqualizerAPO(SpatialRemapping.GetMatrix(ChannelPrototype.ToLayout(ChannelPrototype.ref710),
                ChannelPrototype.ToLayout(ChannelPrototype.ref512)));
            Assert.AreEqual(expected, result);
        }
    }
}