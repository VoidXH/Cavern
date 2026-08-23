using Cavern;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Remapping;

using Test.Cavern.Consts;

namespace Test.Cavern.Remapping;

/// <summary>
/// Tests the <see cref="CavernizeUpmixer"/> class.
/// </summary>
[TestClass]
public class CavernizeUpmixer_Tests {
    /// <summary>
    /// Generate one <see cref="Source"/> for each channel in the <see cref="Listener"/> with the given <paramref name="signal"/> and <paramref name="sampleRate"/>.
    /// </summary>
    static Source[] TestInput(int sampleRate, float[] signal) {
        Channel[] channels = Listener.Channels;
        Source[] result = new Source[channels.Length];
        Clip clip = new(signal, 1, sampleRate);
        for (int i = 0; i < channels.Length; i++) {
            result[i] = new Source {
                Clip = clip,
                LFE = channels[i].LFE,
                VolumeRolloff = Rolloffs.Disabled,
                Position = channels[i].SpatialPos,
            };
        }
        return result;
    }

    /// <summary>
    /// Tests if the first generated <see cref="Source"/>s are at the locations of the originals.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void BaseChannelsStay51() {
        // Arrange: set up a 5.1 listener and input
        Listener.ReplaceChannels(6);
        Channel[] setupChannels = Listener.Channels;
        Source[] inputSources = new Source[setupChannels.Length];
        for (int i = 0; i < setupChannels.Length; i++) {
            inputSources[i] = new Source {
                LFE = setupChannels[i].LFE,
                Position = setupChannels[i].SpatialPos
            };
        }

        // Act: create the upmixer
        CavernizeUpmixer upmixer = new(inputSources, Listener.DefaultSampleRate);
        Source[] intermediate = upmixer.IntermediateSources;

        // Assert: check if the ground sources are at their correct (input) positions
        for (int i = 0; i < setupChannels.Length; i++) {
            Assert.AreEqual(setupChannels[i].SpatialPos, intermediate[i].Position);
        }
    }

    /// <summary>
    /// Tests that a 5.1 input with an LFE signal produces LFE output, and the <see cref="Listener"/>
    /// has 1 LFE-tagged <see cref="Source"/> in <see cref="Listener.ActiveSources"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LFEOutput_When51InputHasLFESignal() {
        const int lfeIndex = 3; // Index of the LFE channel in 5.1 layout

        // Arrange: set up a 5.1 listener and input
        Listener.ReplaceChannels(6);
        Listener listener = new(false) {
            Normalizer = 0,
            LFESeparation = true,
            DirectLFE = true
        };
        float[] testSignal = Generators.DiracDeltaOffset(67);
        Source[] inputSources = TestInput(listener.SampleRate, testSignal);

        // Act: create the upmixer with the 5.1 input sources and trigger a rendered frame
        CavernizeUpmixer upmixer = new CavernizeUpmixer(inputSources, listener.SampleRate);
        listener.AttachSources(upmixer.IntermediateSources);
        float[] output = listener.Render();
        int updateRate = listener.UpdateRate;
        Assert.AreEqual(Listener.Channels.Length * updateRate, output.Length);

        // Assert: verify there is LFE output in the rendered channel data
        for (int sample = 0; sample < updateRate; sample++) {
            float value = output[Listener.Channels.Length * sample + lfeIndex];
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

    /// <summary>
    /// Tests if all upmixed intermediate <see cref="Source"/>s are elevated at least a tiny bit.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void UpmixedSourcesMove() {
        // Arrange: set up a 5.1 listener and input
        Listener.ReplaceChannels(6);
        Listener listener = new(false);
        float[] testSignal = WaveformGenerator.Sine(50000, 240, Listener.DefaultSampleRate);
        Source[] inputSources = TestInput(listener.SampleRate, testSignal);

        // Act: create the upmixer with the 5.1 input sources and trigger a rendered frame
        CavernizeUpmixer upmixer = new CavernizeUpmixer(inputSources, listener.SampleRate) {
            CenterStays = false
        };
        Source[] intermediates = upmixer.IntermediateSources;
        listener.AttachSources(intermediates);
        listener.Render();

        // Assert: all upmixed sources should have been elevated
        for (int i = Listener.Channels.Length; i < intermediates.Length; i++) {
            Assert.AreNotEqual(0, intermediates[i].Position.Y, $"Intermediate {i} should have been elevated.");
        }
    }
}
