using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Exceptions;
using Cavern.Format.FilterSet;
using Cavern.Format.JSON;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests for the <see cref="MultEQXFilterSet"/> class.
/// </summary>
[TestClass]
public class MultEQXFilterSet_Tests {
    /// <summary>
    /// Tests if all the fields of a MultEQ-X file are present in the exported JSON, taking a true export as a reference.
    /// </summary>
    [TestMethod, Timeout(5000)]
    public void AllFieldsPresent() {
        string path = Path.Combine(Constants.testData, "Configurations", "MultEQ-X Marantz AV7706.mqx");
        JsonFile reference = new JsonFile(File.ReadAllText(path));

        MultEQXFilterSet set = MultEQXFilterSet.FromFile(path);
        BiquadFilter[] bypass = [];
        for (int i = 0; i < set.ChannelCount; i++) {
            set.SetupChannel(i, bypass); // No EQ
        }
        JsonFile export = new JsonFile(set.Export());

        Assert.IsTrue(export.IsSupersetOf(reference, true));
    }

    /// <summary>
    /// Tests loading a real MultEQ-X configuration file.
    /// </summary>
    [TestMethod, Timeout(5000)]
    public void ImportFromFile() {
        string path = Path.Combine(Constants.testData, "Configurations", "MultEQ-X Marantz AV7706.mqx");
        MultEQXFilterSet set = MultEQXFilterSet.FromFile(path);

        Assert.IsTrue(set.Valid);
        Assert.AreEqual(12, set.Channels.Length);
        ReferenceChannel[] expected = [
            ReferenceChannel.FrontLeft,
            ReferenceChannel.FrontRight,
            ReferenceChannel.FrontCenter,
            ReferenceChannel.SideLeft,
            ReferenceChannel.SideRight,
            ReferenceChannel.RearLeft,
            ReferenceChannel.RearRight,
            ReferenceChannel.TopFrontLeft,
            ReferenceChannel.TopFrontRight,
            ReferenceChannel.TopRearLeft,
            ReferenceChannel.TopRearRight,
            ReferenceChannel.ScreenLFE
        ];
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i], set.Channels[i].reference, $"Mismatch at channel {i}");
        }
    }

    /// <summary>
    /// Tests creating a complete MultEQ-X file from scratch and exporting it.
    /// </summary>
    [TestMethod, Timeout(2000)]
    public void ExportFullFileFromScratch() {
        MultEQXFilterSet set = new([ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight], 48000);
        set.SetupChannel(0, [new PeakingEQ(48000, 1000, -3, 1.41)]);
        set.SetupChannel(1, [new HighShelf(48000, 10000, 2, 0.71)]);

        JsonFile exported = new JsonFile(set.Export());
        Assert.IsTrue(exported.ContainsKey("_measurements"));
        Assert.IsTrue(exported.ContainsKey("_channelDataMap"));
        Assert.IsTrue(exported.ContainsKey("OrderedChannelGuids"));
        Assert.IsTrue(exported.ContainsKey("CalibrationSettings"));
        Assert.IsTrue(exported.ContainsKey("TargetCurveSet"));
        Assert.IsTrue(exported.ContainsKey("PositionNames"));
        Assert.IsTrue(exported.ContainsKey("UsedLocalMicrophones"));

        object[] orderedGuids = (object[])exported["OrderedChannelGuids"];
        Assert.AreEqual(2, orderedGuids.Length);

        object[] targets = (object[])exported["TargetCurveSet"];
        Assert.AreEqual(2, targets.Length);

        // Verify stream export
        using MemoryStream stream = new();
        set.Export(stream);
        string jsonText = Encoding.UTF8.GetString(stream.ToArray());
        Assert.IsFalse(string.IsNullOrEmpty(jsonText));

        // Verify round-trip import
        MultEQXFilterSet imported = MultEQXFilterSet.FromString(jsonText);
        Assert.AreEqual(2, imported.Channels.Length);
        Assert.AreEqual(ReferenceChannel.FrontLeft, imported.Channels[0].reference);
        Assert.AreEqual(ReferenceChannel.FrontRight, imported.Channels[1].reference);
    }

    /// <summary>
    /// Tests that corrupt JSON throws CorruptionException.
    /// </summary>
    [TestMethod, Timeout(1000)]
    [ExpectedException(typeof(CorruptionException))]
    public void ImportCorrupted() => MultEQXFilterSet.FromString("{}");
}
