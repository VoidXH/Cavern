using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Utilities;

using Test.Cavern.Consts;

namespace Test.Cavern.Filters;

/// <summary>
/// Tests the <see cref="Gain"/> filters and its <see cref="GainAmp"/> counterpart.
/// </summary>
[TestClass]
public class Gain_Tests {
    /// <summary>
    /// Tests if <see cref="Gain"/> works correctly for a mono signal.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FastConvolverMono() => CavernAmpTest.Run(() => {
        using IGainFilter filter = FilterFactory.CreateGain(gainDb);
        float[] signal = [1, 1];
        filter.Process(signal);
        TestUtils.AssertAll(signal, (float)gain);
    });

    /// <summary>
    /// Tests if <see cref="Gain"/> only affects a single channel in a stereo signal.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FastConvolverStereo() => CavernAmpTest.Run(() => {
        using IGainFilter filter = FilterFactory.CreateGain(gainDb);
        float[] signal = [1, 1];
        filter.Process(signal, 0, 2);
        Assert.AreEqual(gain, signal[0]);
        Assert.AreEqual(1, signal[1]);
    });

    /// <summary>
    /// Gain used for the tests and value checks.
    /// </summary>
    const double gain = .5f;

    /// <summary>
    /// Gain used for the tests, when it's expected in decibels.
    /// </summary>
    static readonly double gainDb = QMath.GainToDb(gain);
}
