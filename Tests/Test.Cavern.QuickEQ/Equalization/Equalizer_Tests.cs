using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Equalization;

/// <summary>
/// Tests the <see cref="Equalizer"/> class.
/// </summary>
[TestClass]
public class Equalizer_Tests {
    /// <summary>
    /// Tests if <see cref="Equalizer.AddSlope(double, double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void AddSlope() {
        Equalizer equalizer = Create(20, 40, 80, 500, 640, 1280, 3000);
        equalizer.AddSlope(3, 40, 2560);
        Compare(equalizer, 0, 0, 3, 10.931568423390376, 12, 15, 18);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.GetAverageLevel(double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetAverageLevel() {
        Equalizer equalizer = Create(20, 20000, 100, 10);
        Assert.AreEqual(8.760460922297078, equalizer.GetAverageLevel(100, 500), Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.GetValleys(double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetValleys() {
        Equalizer equalizer = Create(20, 20000, 100, 10);
        equalizer.DownsampleLogarithmically(1024, 20, 20000);
        List<(int startInclusive, int stopExclusive)> valleys = equalizer.GetValleys(10, 1);
        Assert.AreEqual(15, valleys.Count);
        Assert.AreEqual(488, valleys[0].startInclusive);
        Assert.AreEqual(617, valleys[0].stopExclusive);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.LimitPeaks(double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LimitPeaks() {
        Equalizer equalizer = Create(20, 20000, 10, 10);
        equalizer.LimitPeaks(3);
        Compare(equalizer, 0, 3, 3, 1.4112, -7.568025, -9.589243, -2.794155, 3, 3, 3);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.LimitPeaks(double, double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LimitPeaksRange() {
        Equalizer equalizer = Create(20, 20000, 10, 10);
        equalizer.LimitPeaks(6, 0, 10000);
        Compare(equalizer, 0, 6, 6, 1.4112, -7.568025, -9.589243, -2.794155, 6.569866, 9.893582, 4.121185);
        equalizer.LimitPeaks(3, 10000, 100000);
        Compare(equalizer, 0, 6, 6, 1.4112, -7.568025, -9.589243, -2.794155, 3, 3, 3);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.MonotonousDecrease(double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void MonotonousDecrease() {
        Equalizer equalizer = Create(20, 20000, 100, 10);
        equalizer.MonotonousDecrease(0, 10000);
        Assert.IsTrue(equalizer.Bands.Where(x => x.Frequency < 5000).All(x => x.Gain > .99));
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.Smooth(double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Smooth() {
        Equalizer equalizer = Create(20, 20000, 100, 10);
        equalizer.Smooth(2);
        Assert.IsTrue(equalizer.Bands.All(x => Math.Abs(x.Gain) < 5));
    }

    /// <summary>
    /// Create an <see cref="Equalizer"/> that resembles a sine wave of a given amplitude.
    /// </summary>
    static Equalizer Create(double startFreq, double endFreq, int length, double amplitude) {
        Equalizer result = new();
        double mul = 1.0 / (length - 1);
        for (int i = 0; i < length; i++) {
            result.AddBand(new(QMath.Lerp(startFreq, endFreq, i * mul), amplitude * Math.Sin(i)));
        }
        return result;
    }

    /// <summary>
    /// Create a flat <see cref="Equalizer"/> with given frequencies.
    /// </summary>
    static Equalizer Create(params double[] frequencies) {
        Equalizer result = new();
        for (int i = 0; i < frequencies.Length; i++) {
            result.AddBand(new Band(frequencies[i], 0));
        }
        return result;
    }

    /// <summary>
    /// Assert if all bands have the gain they should after the tested operation.
    /// </summary>
    static void Compare(Equalizer source, params double[] bandGains) {
        IReadOnlyList<Band> bands = source.Bands;
        for (int i = 0; i < bandGains.Length; i++) {
            Assert.AreEqual(bands[i].Gain, bandGains[i], Constants.delta);
        }
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.FromBase64(string)"/> can deserialize a flat equalizer.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FromBase64Flat() {
        Equalizer original = new Equalizer();
        original.AddBand(new Band(1000, 0));
        string base64 = original.ToBase64();
        Equalizer deserialized = new Equalizer();
        deserialized.FromBase64(base64);
        Assert.AreEqual(1, deserialized.Bands.Count);
        Assert.AreEqual(0, deserialized.Bands[0].Gain, Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.ToBase64"/> produces a valid base64 string.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToBase64ProducesValidString() {
        Equalizer equalizer = Create(20, 20000, 100, 10);
        string base64 = equalizer.ToBase64();
        Assert.IsFalse(string.IsNullOrEmpty(base64));
        byte[] data = Convert.FromBase64String(base64);
        Assert.IsTrue(data.Length > 0);
    }

    /// <summary>
    /// Tests if <see cref="Equalizer.ToBase64"/> and <see cref="Equalizer.FromBase64(string)"/> round-trip correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SerializationRoundTrip() {
        Equalizer original = Create(20, 20000, 100, 10);
        string base64 = original.ToBase64();
        Equalizer deserialized = new Equalizer();
        deserialized.FromBase64(base64);
        Assert.AreEqual(original.Bands.Count, deserialized.Bands.Count);
        for (int i = 0; i < original.Bands.Count; i++) {
            Assert.AreEqual(original.Bands[i].Frequency, deserialized.Bands[i].Frequency, Constants.delta);
            Assert.AreEqual(original.Bands[i].Gain, deserialized.Bands[i].Gain, Constants.delta);
        }
    }
}
