using Cavern.QuickEQ.Equalization;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Equalization;

/// <summary>
/// Tests the <see cref="EQGenerator"/> class's transform functions.
/// </summary>
[TestClass]
public class EQGeneratorTransform_Tests {
    /// <summary>
    /// Tests if <see cref="EQGenerator.Fade(Equalizer, Equalizer, double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Fade() {
        Equalizer low = new Equalizer([new Band(100, 0), new Band(200, 0), new Band(400, 0), new Band(800, 0), new Band(1600, 0)], true),
            high = new Equalizer([new Band(100, 6), new Band(200, 6), new Band(400, 6), new Band(800, 6), new Band(1600, 6)], true),
            result = EQGenerator.Fade(low, high, 400, 2);
        Assert.AreEqual(5, result.Bands.Count);
        Assert.AreEqual(100, result.Bands[0].Frequency, Constants.delta);
        Assert.AreEqual(0, result.Bands[0].Gain, Constants.delta);
        Assert.AreEqual(200, result.Bands[1].Frequency, Constants.delta);
        Assert.AreEqual(0, result.Bands[1].Gain, Constants.delta);
        Assert.AreEqual(400, result.Bands[2].Frequency, Constants.delta);
        Assert.AreEqual(3, result.Bands[2].Gain, Constants.delta);
        Assert.AreEqual(800, result.Bands[3].Frequency, Constants.delta);
        Assert.AreEqual(6, result.Bands[3].Gain, Constants.delta);
        Assert.AreEqual(1600, result.Bands[4].Frequency, Constants.delta);
        Assert.AreEqual(6, result.Bands[4].Gain, Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="EQGenerator.Fade(Equalizer, Equalizer, double, double)"/> returns a clone of the high curve when the transition is out of range.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Fade_NoOverlap() {
        Equalizer low = new Equalizer([new Band(10, 0), new Band(20, 0)], true),
            high = new Equalizer([new Band(10, 6), new Band(20, 6)], true),
            result = EQGenerator.Fade(low, high, 1000, 1);
        Assert.AreEqual(2, result.Bands.Count);
        Assert.AreEqual(10, result.Bands[0].Frequency, Constants.delta);
        Assert.AreEqual(6, result.Bands[0].Gain, Constants.delta);
        Assert.AreEqual(20, result.Bands[1].Frequency, Constants.delta);
        Assert.AreEqual(6, result.Bands[1].Gain, Constants.delta);
    }
}
