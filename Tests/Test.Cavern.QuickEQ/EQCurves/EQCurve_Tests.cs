using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.Equalization;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.EQCurves;

/// <summary>
/// Tests the <see cref="EQCurve"/> class.
/// </summary>
[TestClass]
public class EQCurve_Tests {
    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="AutoRoomCurve"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeAutoRoomCurve() {
        Equalizer[] responses = [new Equalizer([new(300, 0), new(500, 6), new(700, 0)], true)];
        AutoRoomCurve curve = new AutoRoomCurve(responses);
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(RoomCurveLikeCurve));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="Bandpass"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeBandpass() {
        Bandpass curve = new Bandpass(20, 20000, 48000, 1024);
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(Bandpass));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="Custom"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeCustom() {
        Equalizer eq = new Equalizer([
            new(300, 0), new(500, 6), new(700, 0)
        ], true);
        Custom curve = new Custom(eq);
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(Custom));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="Depth"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeDepth() {
        Depth curve = new Depth();
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(Depth));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="Flat"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeFlat() {
        Flat curve = new Flat();
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(Flat));
        Assert.AreEqual(0, deserialized[1000], Constants.delta);
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="Punch"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializePunch() {
        Punch curve = new Punch();
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(Punch));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes a <see cref="RoomCurve"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeRoomCurve() {
        RoomCurve curve = new RoomCurve();
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(RoomCurveLikeCurve));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.Deserialize(string)"/> correctly deserializes an <see cref="XCurve"/> curve.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DeserializeXCurve() {
        XCurve curve = new XCurve();
        string base64 = curve.ToBase64();
        EQCurve deserialized = EQCurve.Deserialize(base64);
        Assert.IsInstanceOfType(deserialized, typeof(XCurve));
    }

    /// <summary>
    /// Tests if <see cref="EQCurve.GetAverageLevel(double, double, double)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetAverageLevel() {
        EQCurve curve = new Punch(3);
        Assert.AreEqual(1.8299298951978702, curve.GetAverageLevel(20, 120, 10), Constants.delta);
        Assert.AreEqual(0, curve.GetAverageLevel(200, 1000, 100), Constants.delta);
        Assert.AreEqual(-0.34931992656327837, curve.GetAverageLevel(1000, 2000, 100), Constants.delta);
    }
}
