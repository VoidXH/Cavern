using Cavern;
using Cavern.QuickEQ.Crossover;
using Cavern.QuickEQ.Crossover.Enums;

using Test.Cavern.QuickEQ.Consts;

using CrossoverBase = Cavern.QuickEQ.Crossover.Crossover;

namespace Test.Cavern.QuickEQ.Crossover;

/// <summary>
/// Tests the static factory methods of the <see cref="Crossover"/> base class.
/// </summary>
[TestClass]
public class Crossover_Tests {
    /// <summary>
    /// Tests if <see cref="Crossover.Create(CrossoverType, CrossoverDescription)"/> with Biquad type
    /// produces a 2nd-order Butterworth by comparing FIR output against a manually constructed
    /// <see cref="BasicCrossover"/>. Must be a 100% float match.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Create_Biquad_DefaultIs2ndOrderButterworth() {
        CrossoverDescription mixing = Crossovers.Description5_1;
        const int length = 256;

        CrossoverBase factory = CrossoverBase.Create(CrossoverType.Biquad, mixing);
        BasicCrossover expected = new BasicCrossover(mixing);

        float[] factoryHigh = factory.GetHighpass(Listener.DefaultSampleRate, frequency, length);
        float[] expectedHigh = expected.GetHighpass(Listener.DefaultSampleRate, frequency, length);

        TestUtils.AssertArrayEquals(expectedHigh, factoryHigh, 0);
    }

    /// <summary>
    /// Tests if <see cref="Crossover.Create(CrossoverType, CrossoverDescription, int, CrossoverSlope)"/>
    /// with custom order and slope matches a manually constructed <see cref="BasicCrossover"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Create_Biquad_CustomOrderAndSlope() {
        CrossoverDescription mixing = Crossovers.Description5_1;
        const int length = 256;
        const int order = 4;
        const CrossoverSlope slope = CrossoverSlope.LinkwitzRiley;

        CrossoverBase factory = CrossoverBase.Create(CrossoverType.Biquad, mixing, order, slope);
        BasicCrossover expected = new BasicCrossover(mixing, order, slope);

        float[] factoryHigh = factory.GetHighpass(Listener.DefaultSampleRate, frequency, length);
        float[] expectedHigh = expected.GetHighpass(Listener.DefaultSampleRate, frequency, length);

        TestUtils.AssertArrayEquals(expectedHigh, factoryHigh, 0);
    }

    /// <summary>
    /// Tests if <see cref="Crossover.Create(CrossoverType, CrossoverDescription)"/> with Cavern type
    /// produces a valid <see cref="CavernCrossover"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Create_Cavern_ReturnsCavernCrossover() {
        CrossoverBase crossover = CrossoverBase.Create(CrossoverType.Cavern, Crossovers.Description5_1);
        Assert.IsInstanceOfType(crossover, typeof(CavernCrossover));
        Assert.AreEqual(CrossoverType.Cavern, crossover.Type);
    }

    /// <summary>
    /// Tests if <see cref="Crossover.Create(CrossoverType, CrossoverDescription)"/> with SyntheticBiquad type
    /// produces a valid <see cref="SyntheticBiquadCrossover"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Create_SyntheticBiquad_ReturnsSyntheticBiquadCrossover() {
        CrossoverBase crossover = CrossoverBase.Create(CrossoverType.SyntheticBiquad, Crossovers.Description5_1);
        Assert.IsInstanceOfType(crossover, typeof(SyntheticBiquadCrossover));
        Assert.AreEqual(CrossoverType.SyntheticBiquad, crossover.Type);
    }

    /// <summary>
    /// Tests if <see cref="Crossover.Create(CrossoverType, CrossoverDescription)"/> with Disabled type
    /// produces a valid <see cref="DisabledCrossover"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Create_Disabled_ReturnsDisabledCrossover() {
        CrossoverBase crossover = CrossoverBase.Create(CrossoverType.Disabled, Crossovers.Description5_1);
        Assert.IsInstanceOfType(crossover, typeof(DisabledCrossover));
        Assert.AreEqual(CrossoverType.Disabled, crossover.Type);
    }

    /// <summary>
    /// Tests if static <see cref="Crossover.GetLowpass"/> delegates to the correct factory.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StaticGetLowpass_MatchesFactory() {
        const int length = 256;
        float[] staticResult = CrossoverBase.GetLowpass(CrossoverType.Biquad, Listener.DefaultSampleRate, frequency, length);
        float[] factoryResult = CrossoverBase.Create(CrossoverType.Biquad, null).GetLowpass(Listener.DefaultSampleRate, frequency, length);

        TestUtils.AssertArrayEquals(staticResult, factoryResult, 0);
    }

    /// <summary>
    /// Tests if static <see cref="Crossover.GetHighpass"/> delegates to the correct factory.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StaticGetHighpass_MatchesFactory() {
        const int length = 256;
        float[] staticResult = CrossoverBase.GetHighpass(CrossoverType.Biquad, Listener.DefaultSampleRate, frequency, length);
        float[] factoryResult = CrossoverBase.Create(CrossoverType.Biquad, null).GetHighpass(Listener.DefaultSampleRate, frequency, length);

        TestUtils.AssertArrayEquals(staticResult, factoryResult, 0);
    }

    /// <summary>
    /// Commonly tested crossover frequency.
    /// </summary>
    const float frequency = 1000;
}
