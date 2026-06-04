using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavernize.Logic.Models;

namespace Test.Cavernize.Logic.Models;

/// <summary>
/// Tests the <see cref="AudioFile"/> class.
/// </summary>
[TestClass]
public class AudioFile_Tests {
    /// <summary>
    /// Tests if <see cref="MatroskaReader"/> correctly reads the track list.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Tracks() {
        string input = Path.Combine(Consts.cavernFormatTestData, "Multitrack.mkv");
        using AudioFile file = new(input, new());
        Track[] tracks = [.. file.AllTracks];
        Assert.AreEqual(4, tracks.Length);
        Assert.AreEqual(Codec.HEVC, tracks[0].Format);
        Assert.AreEqual(Codec.Unknown, tracks[1].Format);
        Assert.AreEqual(Codec.EnhancedAC3, tracks[2].Format);
        Assert.AreEqual(Codec.EnhancedAC3, tracks[3].Format);
    }
}
