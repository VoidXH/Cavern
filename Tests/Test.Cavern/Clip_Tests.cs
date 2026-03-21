using Cavern;

using Test.Cavern.Consts;

namespace Test.Cavern;

/// <summary>
/// Tests the <see cref="Clip"/> class.
/// </summary>
[TestClass]
public class Clip_Tests {
    /// <summary>
    /// Tests if a mono clip's samples can be assigned and queried with.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SetGetData_Mono() {
        Clip clip = new(new float[Constants.samples.Length], 1, Listener.DefaultSampleRate);
        Assert.IsTrue(clip.SetData(Constants.samples, loopOffset));

        float[] getTo = new float[Constants.samples.Length];
        Assert.IsTrue(clip.GetData(getTo, loopOffset));
        CollectionAssert.AreEqual(Constants.samples, getTo);

        Assert.IsTrue(clip.GetDataNonLooping(getTo, loopOffset));
        CollectionAssert.AreEqual(Constants.samples[..^loopOffset], getTo[..(Constants.samples.Length - loopOffset)]);
    }

    /// <summary>
    /// Tests if a stereo clip's samples can be assigned and queried with.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SetGetData_Multichannel() {
        Clip clip = new(new(
            new float[Constants.samples.Length],
            new float[Constants.samples.Length]
        ), Listener.DefaultSampleRate);
        Assert.IsTrue(clip.SetData(Constants.multichannel, loopOffset));

        float[] getTo = new float[Constants.samples.Length];
        Assert.IsTrue(clip.GetData(getTo, 0, loopOffset));
        CollectionAssert.AreEqual(Constants.samples, getTo);

        MultichannelWaveform getMultichannelTo = new(new float[Constants.samples.Length], new float[Constants.samples.Length]);
        Assert.IsTrue(clip.GetData(getMultichannelTo, loopOffset));
        CollectionAssert.AreEqual(Constants.samples, getMultichannelTo[0]);

        Assert.IsTrue(clip.GetDataNonLooping(getMultichannelTo, loopOffset));
        CollectionAssert.AreEqual(Constants.samples2[..^loopOffset], getMultichannelTo[1][..(Constants.samples.Length - loopOffset)]);
    }

    /// <summary>
    /// In looping functions, set the offset to this many samples to check if the looping works.
    /// </summary>
    const int loopOffset = 2;
}
