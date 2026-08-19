using Cavern;
using Cavern.Channels;
using Cavern.Utilities;
using Cavern.Waveforms;

using Test.Cavern.Consts;

namespace Test.Cavern.Utilities;

/// <summary>
/// Tests the <see cref="WaveformUtils"/> class.
/// </summary>
[TestClass]
public class WaveformUtils_Tests {
    /// <summary>
    /// Tests if <see cref="WaveformUtils.Delay(float[], float)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_Subsample() => CavernAmpTest.Run(() => {
        float[] impulse = new float[128];
        impulse[0] = 1;
        WaveformUtils.Delay(impulse, 64f);
        Assert.AreEqual(impulse[64], 1);
    });

    /// <summary>
    /// Tests if <see cref="WaveformUtils.Downmix(float[], int)"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Downmix() {
        float[] downmix = Constants.stereoSamples.Downmix(2);
        Assert.AreEqual(.2f, downmix[0]);
        Assert.AreEqual(.2f, downmix[1]);
        Assert.AreEqual(.4f, downmix[2]);
        Assert.AreEqual(.6f, downmix[3]);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.Downmix(float[], float[], int)"/> works for playing quadro on a 5.1 system.
    /// It's called downmix as it's mostly downmixing, but actually mixes to a channel count without knowledge of their layout.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DownmixUp() {
        Listener.ReplaceChannels(ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(4)));
        float[] result = new float[Constants.stereoSamples.Length / 4 * 6];
        WaveformUtils.Downmix(Constants.stereoSamples, result, 6);
        float[] expected = [.1f, .1f, 0, 0, 0, .2f, .1f, .3f, 0, 0, .1f, .5f];
        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.GetPeak(float[])"/> works at any index.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetPeak() {
        float[] source = new float[3];
        for (int i = 0; i < source.Length;) {
            source[i] = i + 1;
            Assert.AreEqual(++i, (int)source.GetPeak());
        }
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.TrimEnd(float[][])"/> correctly cuts the end of jagged arrays.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimEnd_2D() {
        MultichannelWaveform source = new(
            new float[100], // Will be cut until the nicest element
            new float[100] // Will be empty, but not cut, since the other jagged array is longer
        );
        source[0][Constants.nice] = 1;
        source.TrimEnd();

        Assert.AreEqual(Constants.nice + 1, source[0].Length);
        Assert.AreEqual(source[0].Length, source[1].Length);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], float[], int, int)"/> extracts a single channel correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_Basic() {
        // Interlaced stereo: L0, R0, L1, R1, L2, R2, L3, R3
        float[] interlaced = [1, 10, 2, 20, 3, 30, 4, 40];
        float[] left = new float[4];
        float[] right = new float[4];

        WaveformUtils.ExtractChannel(interlaced, left, 0, 2);
        WaveformUtils.ExtractChannel(interlaced, right, 1, 2);

        CollectionAssert.AreEqual(new float[] { 1, 2, 3, 4 }, left);
        CollectionAssert.AreEqual(new float[] { 10, 20, 30, 40 }, right);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], float[], int, int)"/> works with quadro (4 channels).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_Quadro() {
        // 4 channels, 3 samples each = 12 total
        float[] interlaced = new float[12];
        for (int i = 0; i < interlaced.Length; i++) {
            interlaced[i] = i + 1;
        }
        // Layout: C0, C1, C2, C3, C0, C1, C2, C3, C0, C1, C2, C3

        float[] ch0 = new float[3];
        float[] ch1 = new float[3];
        float[] ch2 = new float[3];
        float[] ch3 = new float[3];

        WaveformUtils.ExtractChannel(interlaced, ch0, 0, 4);
        WaveformUtils.ExtractChannel(interlaced, ch1, 1, 4);
        WaveformUtils.ExtractChannel(interlaced, ch2, 2, 4);
        WaveformUtils.ExtractChannel(interlaced, ch3, 3, 4);

        CollectionAssert.AreEqual(new float[] { 1, 5, 9 }, ch0);
        CollectionAssert.AreEqual(new float[] { 2, 6, 10 }, ch1);
        CollectionAssert.AreEqual(new float[] { 3, 7, 11 }, ch2);
        CollectionAssert.AreEqual(new float[] { 4, 8, 12 }, ch3);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], float[], int, int)"/> handles destination shorter than source.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_DestinationShorter() {
        float[] interlaced = [1, 10, 2, 20, 3, 30, 4, 40, 5, 50]; // 5 samples per channel
        float[] left = new float[3]; // Only want 3 samples

        WaveformUtils.ExtractChannel(interlaced, left, 0, 2);

        CollectionAssert.AreEqual(new float[] { 1, 2, 3 }, left);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], float[], int, int)"/> handles source shorter than destination.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_SourceShorter() {
        float[] interlaced = [1, 10, 2, 20]; // 2 samples per channel
        float[] left = new float[5]; // Destination larger

        WaveformUtils.ExtractChannel(interlaced, left, 0, 2);

        // Only first 2 elements should be filled, rest remain 0
        CollectionAssert.AreEqual(new float[] { 1, 2, 0, 0, 0 }, left);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], long, float[], int, int)"/> extracts a channel with an offset.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_WithOffset() {
        // Interlaced stereo: L0, R0, L1, R1, L2, R2, L3, R3
        float[] interlaced = [1, 10, 2, 20, 3, 30, 4, 40];
        float[] left = new float[2];

        // Start at sample 2 (which is L1), extract 2 samples
        WaveformUtils.ExtractChannel(interlaced, 2, left, 0, 2);

        CollectionAssert.AreEqual(new float[] { 2, 3 }, left);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], long, float[], int, int)"/> works with offset at different positions.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_WithOffset_MultipleChannels() {
        // 4 channels, 3 samples each
        float[] interlaced = new float[12];
        for (int i = 0; i < interlaced.Length; i++) {
            interlaced[i] = i + 1;
        }

        float[] ch2 = new float[2];
        // Start at offset 4 (second sample across all channels), extract channel 2
        // Layout: C0, C1, C2, C3 | C0, C1, C2, C3 | C0, C1, C2, C3
        // Offset 4 means start at the second block, channel 0. Adding channel 2 = index 6.
        WaveformUtils.ExtractChannel(interlaced, 4, ch2, 2, 4);

        CollectionAssert.AreEqual(new float[] { 7, 11 }, ch2);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], long, float[], int, int)"/> handles offset at the end of source.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_WithOffset_AtEnd() {
        float[] interlaced = [1, 10, 2, 20]; // 2 samples per channel
        float[] left = new float[2];

        // Offset 4 is exactly at the end (2 samples * 2 channels = 4)
        WaveformUtils.ExtractChannel(interlaced, 4, left, 0, 2);

        // Should not write anything, destination remains unchanged
        CollectionAssert.AreEqual(new float[] { 0, 0 }, left);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], int, int, float[], int, int)"/> copies between different channel layouts.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_CrossLayout() {
        // Source: stereo (2 channels), 4 samples each
        float[] stereoSource = [1, 2, 3, 4, 5, 6, 7, 8]; // L0, R0, L1, R1, L2, R2, L3, R3
                                                         // Destination: 5.1 (6 channels), we want to copy left to channel 0, right to channel 1
        float[] surroundDest = new float[6 * 4]; // 4 samples per channel

        // Copy left (channel 0) to destination channel 0
        WaveformUtils.ExtractChannel(stereoSource, 0, 2, surroundDest, 0, 6);
        // Copy right (channel 1) to destination channel 1
        WaveformUtils.ExtractChannel(stereoSource, 1, 2, surroundDest, 1, 6);

        // Check destination channel 0 (was left)
        float[] destCh0 = new float[4];
        WaveformUtils.ExtractChannel(surroundDest, destCh0, 0, 6);
        CollectionAssert.AreEqual(new float[] { 1, 3, 5, 7 }, destCh0);

        // Check destination channel 1 (was right)
        float[] destCh1 = new float[4];
        WaveformUtils.ExtractChannel(surroundDest, destCh1, 1, 6);
        CollectionAssert.AreEqual(new float[] { 2, 4, 6, 8 }, destCh1);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], int, int, float[], int, int)"/> works with different channel counts.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_CrossLayout_DifferentChannelCounts() {
        // Source: mono (1 channel), 4 samples
        float[] monoSource = [1, 2, 3, 4];
        // Destination: stereo (2 channels), copy to both channels
        float[] stereoDest = new float[2 * 4];

        WaveformUtils.ExtractChannel(monoSource, 0, 1, stereoDest, 0, 2);
        WaveformUtils.ExtractChannel(monoSource, 0, 1, stereoDest, 1, 2);

        // Both destination channels should have the mono signal
        float[] destCh0 = new float[4];
        float[] destCh1 = new float[4];
        WaveformUtils.ExtractChannel(stereoDest, destCh0, 0, 2);
        WaveformUtils.ExtractChannel(stereoDest, destCh1, 1, 2);

        CollectionAssert.AreEqual(new float[] { 1, 2, 3, 4 }, destCh0);
        CollectionAssert.AreEqual(new float[] { 1, 2, 3, 4 }, destCh1);
    }

    /// <summary>
    /// Tests if <see cref="WaveformUtils.ExtractChannel(float[], int, int, float[], int, int)"/> copies until the shorter array ends.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExtractChannel_CrossLayout_PartialCopy() {
        // Source: stereo, 6 samples per channel (longer)
        float[] stereoSource = new float[2 * 6];
        for (int i = 0; i < stereoSource.Length; i++) {
            stereoSource[i] = i + 1;
        }
        // Destination: stereo, only 3 samples per channel (shorter)
        float[] stereoDest = new float[2 * 3];

        // Copy from source left channel to dest left channel
        WaveformUtils.ExtractChannel(stereoSource, 0, 2, stereoDest, 0, 2);

        float[] destCh0 = new float[3];
        WaveformUtils.ExtractChannel(stereoDest, destCh0, 0, 2);

        // Only 3 samples copied (limited by destination length)
        CollectionAssert.AreEqual(new float[] { 1, 3, 5 }, destCh0);
    }
}
