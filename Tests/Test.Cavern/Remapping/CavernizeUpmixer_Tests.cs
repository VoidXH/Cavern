using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Remapping;

using Test.Cavern.Consts;

namespace Test.Cavern.Remapping;

/// <summary>
/// Tests the <see cref="CavernizeUpmixer"/> class.
/// </summary>
[TestClass]
public class CavernizeUpmixer_Tests {
    /// <summary>
    /// Tests that a 5.1 input with an LFE signal produces LFE output, and the <see cref="Listener"/>
    /// has 1 LFE-tagged <see cref="Source"/> in <see cref="Listener.ActiveSources"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LFEOutput_When51InputHasLFESignal() {
        const int lfeIndex = 3; // Index of the LFE channel in 5.1 layout

        // Arrange: set up a 5.1 listener
        ReferenceChannel[] layout = ChannelPrototype.ref510;
        Channel[] channels = ChannelPrototype.ToLayout(layout);
        Listener.ReplaceChannels(channels);
        Vector3[] positions = ChannelPrototype.ToPositions(layout);
        Listener listener = new(false) {
            LFESeparation = true,
            DirectLFE = true
        };

        // Create a 5.1 input
        Source[] inputSources = new Source[channels.Length];
        float[] testSignal = Generators.DiracDeltaOffset(67);
        Clip clip = new(testSignal, 1, listener.SampleRate);
        for (int i = 0; i < channels.Length; i++) {
            inputSources[i] = new Source {
                Clip = clip,
                VolumeRolloff = Rolloffs.Disabled,
                LFE = i == lfeIndex,
                Position = positions[i]
            };
        }

        // Act: create the upmixer with the 5.1 input sources
        CavernizeUpmixer upmixer = new CavernizeUpmixer(inputSources, listener.SampleRate);

        // Attach the upmixer's intermediate sources to the listener so Render() processes them
        listener.AttachSources(upmixer.IntermediateSources);

        // Render triggers the full pipeline: input sources are pinged, upmixed, and rendered to channels
        float[] output = listener.Render();
        int updateRate = listener.UpdateRate;
        Assert.AreEqual(channels.Length * updateRate, output.Length);

        // Assert: verify there is LFE output in the rendered channel data
        for (int sample = 0; sample < updateRate; sample++) {
            float value = output[channels.Length * sample + lfeIndex];
            Assert.AreEqual(testSignal[sample], value, Constants.delta, $"LFE channel sample {sample} should match the input signal.");
        }

        // Assert: the Listener should have 1 LFE-tagged Source in ActiveSources
        int lfeCount = 0;
        foreach (Source activeSource in listener.ActiveSources) {
            if (activeSource.LFE) {
                lfeCount++;
            }
        }
        Assert.IsTrue(lfeCount == 1, $"Listener should have 1 LFE-tagged Source in ActiveSources, found {lfeCount}.");
    }
}
