using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

namespace Test.Cavern.QuickEQ.Format.ConfigurationFile {
    /// <summary>
    /// Tests the <see cref="CavernFilterStudioConfigurationFile"/> class.
    /// </summary>
    [TestClass]
    public class CavernFilterStudioConfigurationFile_Tests {
        /// <summary>
        /// Tests if <see cref="CavernFilterStudioConfigurationFile"/> is initialized with the correct channels for 6 channel mode (5.1).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ChannelsAssigned_51() {
            CavernFilterStudioConfigurationFile config = new(string.Empty, 6);
            ReferenceChannel[] reference = ChannelPrototype.ref510;
            (string name, IFilterGraphNode root)[] channels = config.InputChannels;
            for (int i = 0; i < 6; i++) {
                if (channels[i].root.Filter is InputChannel input) {
                    Assert.AreEqual(reference[i], input.Channel);
                } else {
                    Assert.Fail($"Channel {i} doesn't start with an InputChannel filter.");
                }
            }
        }
    }
}
