using Cavern.Format.Common;
using Cavern.Format.Common.Metadata;
using Cavernize.Logic.CommandLine;

using Test.Cavernize.Logic.Utilities;

namespace Test.Cavernize.Logic.CommandLine;

/// <summary>
/// Tests the -trk command line parameter.
/// </summary>
[TestClass]
public class TrackCommand_Tests {
    /// <summary>
    /// Tests if the video tracks are not counted on selection.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void CorrectTrackSelected() {
        MockCavernizeApp app = new MockCavernizeApp();
        CommandLineProcessor.Initialize([
            "-i", Constants.multitrackTestFile,
            "-trk", "1"
        ], app);
        Assert.IsNotNull(app.SelectedTrack);
        Assert.AreEqual(Codec.EnhancedAC3, app.SelectedTrack.Codec);
        if (app.SelectedTrack.Track.Extra is TrackExtraAudio audioExtra) {
            Assert.AreEqual(6, audioExtra.ChannelCount);
        } else {
            Assert.Fail("No audio metadata was parsed.");
        }
    }
}
