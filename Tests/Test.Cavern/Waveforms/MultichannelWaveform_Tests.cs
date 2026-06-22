using Cavern.Waveforms;

using Test.Cavern.Consts;

namespace Test.Cavern.Waveforms;

/// <summary>
/// Tests the <see cref="MultichannelWaveform"/> class.
/// </summary>
[TestClass]
public class MultichannelWaveform_Tests {
    /// <summary>
    /// Tests if construction from multiple mono waveforms works correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_FromArray() {
        MultichannelWaveform wave = new(Constants.stereoSamples, Constants.stereoSamples);
        Assert.AreEqual(2, wave.Channels);
        Assert.AreEqual(Constants.stereoSamples.Length, wave.Length);
        CollectionAssert.AreEqual(Constants.stereoSamples, wave[0]);
        CollectionAssert.AreEqual(Constants.stereoSamples, wave[1]);
    }

    /// <summary>
    /// Tests if construction from an interlaced signal extracts channels correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_Interlaced() {
        float[] interlaced = [1, 2, 3, 4, 5, 6, 7, 8];
        MultichannelWaveform wave = new(interlaced, 2);
        Assert.AreEqual(2, wave.Channels);
        Assert.AreEqual(4, wave.Length);
        Assert.AreEqual(1, wave[0][0]);
        Assert.AreEqual(3, wave[0][1]);
        Assert.AreEqual(2, wave[1][0]);
        Assert.AreEqual(4, wave[1][1]);
    }

    /// <summary>
    /// Tests if construction from an interlaced signal extracts channels correctly with more channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_Interlaced_Quadro() {
        float[] interlaced = new float[16];
        for (int i = 0; i < interlaced.Length; i++) {
            interlaced[i] = i + 1;
        }

        MultichannelWaveform wave = new(interlaced, 4);
        Assert.AreEqual(4, wave.Channels);
        Assert.AreEqual(4, wave.Length);
        Assert.AreEqual(1, wave[0][0]);
        Assert.AreEqual(5, wave[0][1]);
        Assert.AreEqual(2, wave[1][0]);
        Assert.AreEqual(6, wave[1][1]);
    }

    /// <summary>
    /// Tests if construction with zero channels throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_ZeroChannels() {
        try {
            _ = new MultichannelWaveform(0, 10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException) { }
    }

    /// <summary>
    /// Tests if construction with zero samples throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_ZeroSamples() {
        try {
            _ = new MultichannelWaveform(2, 0);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException) { }
    }

    /// <summary>
    /// Tests if construction with negative channel count throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_NegativeChannels() {
        try {
            _ = new MultichannelWaveform(-1, 10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException) { }
    }

    /// <summary>
    /// Tests if construction with negative sample count throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_NegativeSamples() {
        try {
            _ = new MultichannelWaveform(2, -5);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException) { }
    }

    /// <summary>
    /// Tests if construction from differently sized arrays throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Constructor_DifferentLengths() {
        try {
            _ = new MultichannelWaveform(new float[10], new float[5]);
            Assert.Fail("Expected DifferentSignalLengthsException");
        }
        catch (DifferentSignalLengthsException) { }
    }

    /// <summary>
    /// Tests if accessing a channel with an invalid index throws.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Indexer_OutOfBounds() {
        MultichannelWaveform wave = new(2, 10);
        try {
            _ = wave[2];
            Assert.Fail("Expected IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException) { }

        try {
            _ = wave[-1];
            Assert.Fail("Expected IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException) { }
    }

    /// <summary>
    /// Tests if a waveform of zeros is considered mute.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void IsMute_True() {
        MultichannelWaveform wave = new(3, 100);
        Assert.IsTrue(wave.IsMute());
    }

    /// <summary>
    /// Tests if a waveform with any non-zero sample is not mute.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void IsMute_False() {
        MultichannelWaveform wave = new(new float[10], new float[10]);
        wave[0][5] = 0.5f;
        Assert.IsFalse(wave.IsMute());
    }

    /// <summary>
    /// Tests if Gain multiplies all samples by the given factor.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Gain() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5], [0, 1, 2, 3, 4]);
        wave.Gain(2f);
        CollectionAssert.AreEqual(new float[] { 2, 4, 6, 8, 10 }, wave[0]);
        CollectionAssert.AreEqual(new float[] { 0, 2, 4, 6, 8 }, wave[1]);
    }

    /// <summary>
    /// Tests if Gain with zero sets all samples to zero.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Gain_Zero() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5], [6, 7, 8, 9, 10]);
        wave.Gain(0f);
        Assert.IsTrue(wave.IsMute());
    }

    /// <summary>
    /// Tests if GetMonoMix averages all channels correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMonoMix() {
        MultichannelWaveform wave = new([1, 2, 3, 4], [2, 4, 6, 8]);
        float[] mono = wave.GetMonoMix();
        CollectionAssert.AreEqual(new float[] { 3, 6, 9, 12 }, mono);
    }

    /// <summary>
    /// Tests if GetMonoMix of a single channel returns a copy of that channel.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMonoMix_SingleChannel() {
        float[] initial = [1, 2, 3, 4, 5];
        MultichannelWaveform wave = new(initial);
        float[] mono = wave.GetMonoMix();
        CollectionAssert.AreEqual(initial, mono);
    }

    /// <summary>
    /// Tests if GetPeak returns the maximum absolute value across all channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetPeak() {
        MultichannelWaveform wave = new([1, -3, 2, 4, 0], [0, 5, -1, 2, 3]);
        Assert.AreEqual(5, wave.GetPeak());
    }

    /// <summary>
    /// Tests if GetPeak of a single channel works.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetPeak_SingleChannel() {
        MultichannelWaveform wave = new([1, 2, 3, 4, -5]);
        Assert.AreEqual(5, wave.GetPeak());
    }

    /// <summary>
    /// Tests if GetRMS returns the correct RMS value.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetRMS() {
        MultichannelWaveform wave = new([1, 1, 1], [2, 2, 2]);
        float rms = wave.GetRMS();
        Assert.AreEqual(1.5811388f, rms, Constants.delta);
    }

    /// <summary>
    /// Tests if Normalize scales the signal peak to 1.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Normalize() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5], [0, -2, 4, 6, 8]);
        wave.Normalize();
        Assert.AreEqual(1, wave.GetPeak());
    }

    /// <summary>
    /// Tests if Split divides a waveform into blocks of the specified size.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Split() {
        MultichannelWaveform wave = new([0, 1, 2, 3, 4, 5, 6, 7], [8, 9, 10, 11, 12, 13, 14, 15]);
        MultichannelWaveform[] blocks = wave.Split(4);
        Assert.AreEqual(2, blocks.Length);
        Assert.AreEqual(4, blocks[0].Length);
        CollectionAssert.AreEqual(new float[] { 0, 1, 2, 3 }, blocks[0][0]);
        CollectionAssert.AreEqual(new float[] { 8, 9, 10, 11 }, blocks[0][1]);
        CollectionAssert.AreEqual(new float[] { 4, 5, 6, 7 }, blocks[1][0]);
        CollectionAssert.AreEqual(new float[] { 12, 13, 14, 15 }, blocks[1][1]);
    }

    /// <summary>
    /// Tests if Split creates a single block when blockSize equals channel length.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Split_SingleBlock() {
        float[] initial = [1, 2, 3, 4];
        MultichannelWaveform wave = new(initial);
        MultichannelWaveform[] blocks = wave.Split(4);
        Assert.AreEqual(1, blocks.Length);
        CollectionAssert.AreEqual(initial, blocks[0][0]);
    }

    /// <summary>
    /// Tests if TrimStart removes leading zeros from all channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimStart() {
        MultichannelWaveform wave = new([0, 0, 1, 2, 3, 4, 5, 6], [0, 0, 0, 0, 0, 0, 0, 0]);
        wave.TrimStart();
        Assert.AreEqual(6, wave.Length);
        Assert.AreEqual(1, wave[0][0]);
    }

    /// <summary>
    /// Tests if TrimStart handles channels with no leading zeros.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimStart_NoLeadingZeros() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5], [6, 7, 8, 9, 10]);
        wave.TrimStart();
        Assert.AreEqual(5, wave.Length);
    }

    /// <summary>
    /// Tests if TrimEnd removes trailing zeros from all channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimEnd() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5, 6, 7, 0], [8, 9, 10, 11, 12, 13, 14, 15]);
        wave.TrimEnd();
        Assert.AreEqual(8, wave.Length);
    }

    /// <summary>
    /// Tests if TrimEnd trims to the shortest trailing-zero end when both channels have trailing zeros.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimEnd_BothWithZeros() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5, 6, 7, 0], [8, 9, 10, 11, 12, 13, 14, 0]);
        wave.TrimEnd();
        Assert.AreEqual(7, wave.Length);
    }

    /// <summary>
    /// Tests if TrimEnd does nothing when neither channel has trailing zeros.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TrimEnd_NoTrailingZeros() {
        MultichannelWaveform wave = new([1, 2, 3, 4, 5, 6, 7, 8], [9, 10, 11, 12, 13, 14, 15, 16]);
        wave.TrimEnd();
        Assert.AreEqual(8, wave.Length);
    }

    /// <summary>
    /// Tests if Clone returns an independent copy of the waveform.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Clone() {
        float[] initial = [1, 2, 3, 4, 5];
        MultichannelWaveform wave = new(initial);
        MultichannelWaveform cloned = (MultichannelWaveform)wave.Clone();
        CollectionAssert.AreEqual(initial, cloned[0]);

        wave[0][0] = 99;
        Assert.AreEqual(1, cloned[0][0]);
    }

    /// <summary>
    /// Tests if ToArray returns a deep copy of the underlying data.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToArray() {
        float[] initial = [1, 2, 3, 4];
        MultichannelWaveform wave = new(initial);
        float[][] arr = wave.ToArray();
        CollectionAssert.AreEqual(initial, arr[0]);

        arr[0][0] = 99;
        Assert.AreEqual(1, wave[0][0]);
    }
}
