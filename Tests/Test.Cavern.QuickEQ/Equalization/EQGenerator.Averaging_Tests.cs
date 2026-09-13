using Cavern.QuickEQ.Equalization;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Equalization;

/// <summary>
/// Tests the <see cref="EQGenerator"/> class's averaging functions.
/// </summary>
[TestClass]
public class EQGeneratorAveraging_Tests {
    /// <summary>
    /// Tests if <see cref="EQGenerator.AverageRMS(Equalizer[])"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void AverageRMS() {
        Equalizer a = new Equalizer([new Band(20, 1)], true),
            b = new Equalizer([new Band(20, 10)], true),
            avg = EQGenerator.AverageRMS(a, b),
            avg_nodiv = EQGenerator.AverageRMS(a, a);
        Assert.AreEqual(7.50466946361249, avg.PeakGain);
        Assert.AreEqual(a.PeakGain, avg_nodiv.PeakGain, Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="EQGenerator.Average(Equalizer[])"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Average() {
        Equalizer a = new Equalizer([new Band(20, 1), new Band(100, 5)], true),
            b = new Equalizer([new Band(20, 10), new Band(100, 2)], true),
            avg = EQGenerator.Average(a, b),
            avg_nodiv = EQGenerator.Average(a, a);
        Assert.AreEqual(6.61698968676162, avg.Bands[0].Gain);
        Assert.AreEqual(3.6288817004721032, avg.Bands[1].Gain);
        Assert.AreEqual(a.PeakGain, avg_nodiv.PeakGain, Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="EQGenerator.AverageSafe(Equalizer[])"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void AverageSafe() {
        Equalizer a = new Equalizer([new Band(20, 1), new Band(100, 2)], true),
            b = new Equalizer([new Band(50, 3), new Band(200, 4)], true),
            avg = EQGenerator.AverageSafe(a, b);
        Assert.AreEqual(4, avg.Bands.Count);
        Assert.AreEqual(2.0574379391080617, avg.Bands[0].Gain);
        Assert.AreEqual(2.3140848471617477, avg.Bands[1].Gain);
        Assert.AreEqual(2.782339988065255, avg.Bands[2].Gain);
        Assert.AreEqual(3.0574379540092247, avg.Bands[3].Gain);
    }

    /// <summary>
    /// Tests if <see cref="EQGenerator.Max(Equalizer[])"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Max() {
        Equalizer a = new Equalizer([new Band(20, 1), new Band(100, 5)], true),
            b = new Equalizer([new Band(20, 10), new Band(100, 2)], true),
            max = EQGenerator.Max(a, b);
        Assert.AreEqual(10, max.Bands[0].Gain);
        Assert.AreEqual(5, max.Bands[1].Gain);
    }

    /// <summary>
    /// Tests if <see cref="EQGenerator.Min(Equalizer[])"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Min() {
        Equalizer a = new Equalizer([new Band(20, 1), new Band(100, 5)], true),
            b = new Equalizer([new Band(20, 10), new Band(100, 2)], true),
            min = EQGenerator.Min(a, b);
        Assert.AreEqual(1, min.Bands[0].Gain);
        Assert.AreEqual(2, min.Bands[1].Gain);
    }
}
