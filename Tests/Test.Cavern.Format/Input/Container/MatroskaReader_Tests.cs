using Cavern.Format.Common;
using Cavern.Format.Container;

namespace Test.Cavern.Format.Input.Container;

/// <summary>
/// Tests the <see cref="MatroskaReader"/> class.
/// </summary>
[TestClass]
public class MatroskaReader_Tests {
    /// <summary>
    /// Tests if <see cref="MatroskaReader"/> correctly reads the track list.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Tracks() {
        string input = Path.Combine(Consts.testData, "Multitrack.mkv");
        using MatroskaReader reader = new MatroskaReader(input);
        Track[] tracks = reader.Tracks;
        Assert.AreEqual(4, tracks.Length);
        Assert.AreEqual(Codec.HEVC, tracks[0].Format);
        Assert.AreEqual(Codec.Unknown, tracks[1].Format);
        Assert.AreEqual(Codec.EnhancedAC3, tracks[2].Format);
        Assert.AreEqual(Codec.EnhancedAC3, tracks[3].Format);
    }
}
