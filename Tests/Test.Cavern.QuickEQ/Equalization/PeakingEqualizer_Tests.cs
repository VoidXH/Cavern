using Cavern.Filters;
using Cavern.Format;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

using Test.Cavern.Consts;
using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Equalization;

/// <summary>
/// Tests the <see cref="PeakingEqualizer"/> class.
/// </summary>
[TestClass]
public class PeakingEqualizer_Tests {
    /// <summary>
    /// Tests if <see cref="PeakingEqualizer.GetPeakingEQ(int)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void GetPeakingEQ() => CavernAmpTest.Run(() => {
        PeakingEQ[] result = new PeakingEqualizer(Constants.peakAt500Hz).GetPeakingEQ(Constants.sampleRate, 1);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(500, result[0].CenterFreq, 5);
        Assert.AreEqual(6, result[0].Gain, .1f);
    });

    /// <summary>
    /// Tests if <see cref="PeakingEqualizer.GetPeakingEQ(int)"/> uses all bands given for a measurement that could use all the bands given.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void GetPeakingEQ_EnoughBands() => CavernAmpTest.Run(() => {
        AudioReader reader = AudioReader.Open(Constants.fullRange1);
        float[] ir = reader.Read();
        Equalizer eq = EQGenerator.FromTransferFunction(ir.FFT(), reader.SampleRate);
        eq.DownsampleLogarithmically(1024, 20, 20000);
        eq.Smooth(1 / 24f);

        const int count = 6;
        PeakingEQ[] result = new PeakingEqualizer(eq).GetPeakingEQ(Constants.sampleRate, count);
        Assert.AreEqual(count, result.Length);
    });

    /// <summary>
    /// Tests if <see cref="PeakingEqualizer.MaxFrequency"/> is respected across all engines.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void MaxFrequency() => CavernAmpTest.Run(() => {
        const double maxFreq = 400;
        PeakingEQ[] result = new PeakingEqualizer(Constants.peakAt500Hz) {
            MaxFrequency = maxFreq
        }.GetPeakingEQ(Constants.sampleRate, 1);

        if (result.Length == 1) {
            Assert.IsTrue(result[0].CenterFreq < maxFreq);
        } else {
            Assert.AreEqual(0, result.Length);
        }
    });

    /// <summary>
    /// Tests if <see cref="PeakingEqualizer.ParseEQFile(string)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ParseEQFile() {
        PeakingEQ[] result = PeakingEqualizer.ParseEQFile(testEQFile);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(20, result[0].CenterFreq);
        Assert.AreEqual(-2, result[0].Gain);
        Assert.AreEqual(12.5, result[0].Q);
        Assert.AreEqual(20.42, result[1].CenterFreq);
        Assert.AreEqual(1, result[1].Gain);
        Assert.AreEqual(10, result[1].Q);
    }

    static readonly string[] testEQFile = [
        "Equaliser: Generic",
        "Filter  1: ON  PK       Fc   20.00 Hz  Gain  -2.00 dB  Q  12.50",
        "Filter  2: ON  PK       Fc   20.42 Hz  Gain   1.00 dB  Q  10.00"
    ];
}
