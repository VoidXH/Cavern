using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.Utilities;

namespace Test.Cavern.QuickEQ.Format.ConfigurationFile;

/// <summary>
/// Tests the <see cref="EqualizerAPOConfigurationFile"/> class.
/// </summary>
[TestClass]
public class EqualizerAPOConfigurationFile_Tests {
    /// <summary>
    /// Tests if independent Copy filters are merged without merging assignments that depend on them.
    /// </summary>
    [TestMethod]
    public void Export_MergesIndependentCopyFilters() {
        string[] source = [
            "Copy: V1=L+R",
            "Copy: V2=L+R",
            "Copy: V3=V1+V2",
            "Channel: V3",
            "Preamp: -3 dB",
            "Copy: L=V3"
        ];
        EqualizerAPOConfigurationFile configuration = new(source, Listener.DefaultSampleRate);

        List<string> reparsed = configuration.ExportToMemory("Memory.txt").lines;
        Assert.IsTrue(reparsed.Any(line => line.StartsWith("Copy:") && line.Contains("V1=L+R") && line.Contains("V2=L+R")));
        CollectionAssert.Contains(reparsed, "Copy: V3=V1+V2");
        Assert.AreEqual(3, reparsed.Count(line => line.StartsWith("Copy:")));
    }

    /// <summary>
    /// Tests if a graph with shared branches and dependent mixes survives an Equalizer APO memory export and import.
    /// </summary>
    [TestMethod]
    public void Export_RoundTripsWebLikeGraph() {
        EqualizerAPOConfigurationFile configuration = new("Web", 8, true);
        IFilterGraphNode[] inputs = [.. configuration.InputChannels.Select(channel => channel.root)];
        IFilterGraphNode[] outputs = [.. inputs.Select(input => input.Children[0])];
        for (int i = 0; i < 4; i++) {
            inputs[i].DetachChild(outputs[i], false);
        }

        IFilterGraphNode Gain(IFilterGraphNode source, double amount) => source.AddChild(new Gain(amount));
        IFilterGraphNode Mix(string name, params IFilterGraphNode[] sources) {
            FilterGraphNode result = new(new BypassFilter(name));
            for (int i = 0; i < sources.Length; i++) {
                result.AddParent(sources[i]);
            }
            return result;
        }

        IFilterGraphNode left = Gain(inputs[0], -1.2);
        IFilterGraphNode right = Gain(inputs[1], .8);
        IFilterGraphNode center = Gain(inputs[2], -2.4);
        IFilterGraphNode sub = Gain(inputs[3], 1.6);
        IFilterGraphNode frontMix = Gain(Mix("Front mix", left, right), -.5);
        IFilterGraphNode centerMix = Gain(Mix("Center mix", right, center), .3);
        IFilterGraphNode rearMix = Gain(Mix("Rear mix", left, center), -1.1);
        IFilterGraphNode webMix = Gain(Mix("Web mix", frontMix, centerMix, rearMix), .6);
        IFilterGraphNode bassMix = Gain(Mix("Bass mix", webMix, sub), -3);
        outputs[0].AddParent(frontMix);
        outputs[1].AddParent(centerMix);
        outputs[2].AddParent(webMix);
        outputs[3].AddParent(bassMix);

        List<string> lines = configuration.ExportToMemory("Memory.txt").lines;
        Assert.IsTrue(lines.Count(line => line.StartsWith("Copy:")) >= 5, string.Join(Environment.NewLine, lines));
        EqualizerAPOConfigurationFile loaded = new(lines, Listener.DefaultSampleRate);

        ConfigurationFileSimulator source = new(configuration), result = new(loaded);
        int[] activeChannels = [.. Enumerable.Range(0, configuration.InputChannels.Length)];
        for (int channel = 0; channel < configuration.InputChannels.Length; channel++) {
            float[] expected = source.Simulate(activeChannels, 2)[channel];
            float[] actual = result.Simulate(activeChannels, 2)[channel];
            for (int sample = 0; sample < expected.Length; sample++) {
                Assert.AreEqual(expected[sample], actual[sample], .000001, $"Channel {channel}, sample {sample} changed after the round trip.");
            }
        }
    }
}
