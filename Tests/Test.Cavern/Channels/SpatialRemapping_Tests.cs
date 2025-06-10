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
        /// Tests if remapping 2.0 is done correctly and converted to the valid XML output, using the <see cref="Channel"/>-based version.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToXML_Channel() {
            Listener.ReplaceChannels(toXMLSystem);
            string result = SpatialRemapping.ToXML(toXMLContent);
            Assert.AreEqual(File.ReadAllText(Consts.testData + "ToXML.xml"), result);
        }

        /// <summary>
        /// Tests if remapping 2.0 is done correctly and converted to the valid XML output, using the matrix version.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToXML_Matrix() {
            string result = SpatialRemapping.ToXML(SpatialRemapping.GetMatrix(toXMLContent, toXMLSystem));
            Assert.AreEqual(File.ReadAllText(Consts.testData + "ToXML.xml"), result);
        }

        /// <summary>
        /// Tests if remapping 2.0 is done correctly and converted to the valid XML output, using the matrix version.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToXML_Matrix_ExtraParams() {
            string result = SpatialRemapping.ToXML(SpatialRemapping.GetMatrix(toXMLContent, toXMLSystem), ("Test", ["Passed", "Hopefully"]));
            Assert.AreEqual(File.ReadAllText(Consts.testData + "ToXML_ExtraParams.xml"), result);
        }

        /// <summary>
        /// Tests if remapping 7.1 is done correctly to 5.1.2 and converted to the valid Equalizer APO line, using the matrix version.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToEqualizerAPO_Matrix() {
            string result = SpatialRemapping.ToEqualizerAPO(SpatialRemapping.GetMatrix(ChannelPrototype.ToLayout(ChannelPrototype.ref710),
                ChannelPrototype.ToLayout(ChannelPrototype.ref512)));
            Assert.AreEqual(File.ReadAllText(Consts.testData + "ToEqualizerAPO.txt"), result);
        }

        /// <summary>
        /// Tests if remapping 7.1 is done correctly to 5.1.2 and converted to the valid Equalizer APO line, using the matrix version.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToEqualizerAPO_Channel() {
            Listener.ReplaceChannels(ChannelPrototype.ToLayout(ChannelPrototype.ref512));
            string result = SpatialRemapping.ToEqualizerAPO(ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(8)));
            Assert.AreEqual(File.ReadAllText(Consts.testData + "ToEqualizerAPO.txt"), result);
        }

        /// <summary>
        /// System layout used in ToXML tests.
        /// </summary>
        static readonly Channel[] toXMLSystem = ChannelPrototype.ToLayoutAlternative(ChannelPrototype.ref200);

        /// <summary>
        /// Content layout used in ToXML tests.
        /// </summary>
        static readonly Channel[] toXMLContent = ChannelPrototype.ToLayout(ChannelPrototype.ref200);
    }
}