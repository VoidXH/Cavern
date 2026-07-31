using System.Text;

using Cavern.Channels;
using Cavern.Format.FilterSet;
using Cavern.Format.JSON;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests if <see cref="MultEQXT32FilterSet"/> handles legacy and current ADY files.
/// </summary>
[TestClass]
public class MultEQXT32_Tests {
    /// <summary>
    /// Tests export on the current ADY schema.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExportCurrentFixture() => AssertExport(true, [
        ReferenceChannel.FrontLeft,
        ReferenceChannel.FrontCenter,
        ReferenceChannel.FrontRight,
        ReferenceChannel.SideRight,
        ReferenceChannel.SideLeft,
        ReferenceChannel.TopSideRight,
        ReferenceChannel.TopSideLeft,
        ReferenceChannel.ScreenLFE
    ]);

    /// <summary>
    /// Tests export on the legacy ADY schema.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExportLegacyFixture() => AssertExport(false, [
        ReferenceChannel.FrontLeft,
        ReferenceChannel.FrontRight,
        ReferenceChannel.FrontCenter,
        ReferenceChannel.SideLeft,
        ReferenceChannel.SideRight,
        ReferenceChannel.TopFrontLeft,
        ReferenceChannel.TopFrontRight,
        ReferenceChannel.TopRearLeft,
        ReferenceChannel.TopRearRight,
        ReferenceChannel.ScreenLFE
    ]);

    /// <summary>
    /// Run the exporter against an in-memory ADY document.
    /// </summary>
    static void AssertExport(bool includeChannelReports, ReferenceChannel[] channels) {
        JsonFile input = BuildFixture(channels, includeChannelReports);
        MultEQXT32FilterSet set = MultEQXT32FilterSet.FromJson(input, 48000);

        using MemoryStream stream = new();
        set.Export(stream);
        string outputText = Encoding.UTF8.GetString(stream.ToArray());

        JsonFile data = new(outputText);
        Assert.AreEqual("Cavern QuickEQ", data["title"]);

        object[] detectedChannels = (object[])data["detectedChannels"];
        Assert.AreEqual(channels.Length, detectedChannels.Length);

        for (int i = 0; i < detectedChannels.Length; i++) {
            JsonFile channelData = (JsonFile)detectedChannels[i];
            Assert.IsTrue(HasKey(channelData, "channelReport"), $"channelReport missing for detected channel {i}.");
            Assert.IsTrue(HasKey(channelData, "customLevel"), $"customLevel missing for detected channel {i}.");
            Assert.IsTrue(HasKey(channelData, "customDistance"), $"customDistance missing for detected channel {i}.");

            JsonFile channelReport = (JsonFile)channelData["channelReport"];
            Assert.IsTrue(HasKey(channelReport, "distance"), $"distance missing for detected channel {i}.");
            Assert.IsTrue(HasKey(channelReport, "enSpeakerConnect"), $"enSpeakerConnect missing for detected channel {i}.");
        }
    }

    /// <summary>
    /// Build a minimal in-memory ADY-like JSON tree for export tests.
    /// </summary>
    static JsonFile BuildFixture(ReferenceChannel[] channels, bool includeChannelReports) {
        JsonFile data = new() {
            ["detectedChannels"] = channels.Select(channel => {
                JsonFile detectedChannel = new() {
                    ["commandId"] = MapCommandId(channel),
                    ["responseData"] = Array.Empty<object>()
                };
                if (includeChannelReports) {
                    detectedChannel["delayAdjustment"] = "0.0";
                    detectedChannel["trimAdjustment"] = "0.0";
                    detectedChannel["channelReport"] = new JsonFile {
                    { "enSpeakerConnect", 2 },
                    { "isReversePolarity", false },
                    { "distance", 1.25 }
                };
                    detectedChannel["referenceCurveFilter"] = false;
                    detectedChannel["midrangeCompensation"] = true;
                    detectedChannel["frequencyRangeRolloff"] = 20000;
                    detectedChannel["customDistance"] = 1.25;
                    detectedChannel["customLevel"] = "0.0";
                    detectedChannel["customCrossover"] = "80";
                    detectedChannel["customSpeakerType"] = "S";
                    detectedChannel["customTargetCurvePoints"] = Array.Empty<string>();
                }
                return detectedChannel;
            }).ToArray()
        };
        return data;
    }

    /// <summary>
    /// Map a test channel to the identifier used in ADY files.
    /// </summary>
    static string MapCommandId(ReferenceChannel channel) => channel switch {
        ReferenceChannel.FrontLeft => "FL",
        ReferenceChannel.FrontRight => "FR",
        ReferenceChannel.FrontCenter => "C",
        ReferenceChannel.ScreenLFE => "SW1",
        ReferenceChannel.SideLeft => "SLA",
        ReferenceChannel.SideRight => "SRA",
        ReferenceChannel.TopFrontLeft => "TFL",
        ReferenceChannel.TopFrontRight => "TFR",
        ReferenceChannel.TopRearLeft => "TRL",
        ReferenceChannel.TopRearRight => "TRR",
        ReferenceChannel.TopSideLeft => "TML",
        ReferenceChannel.TopSideRight => "TMR",
        _ => throw new NotSupportedException($"Unsupported test channel: {channel}")
    };

    /// <summary>
    /// Check whether a JSON object contains a key without throwing.
    /// </summary>
    static bool HasKey(JsonFile file, string key) => file.Elements.Any(x => x.Key == key);
}
