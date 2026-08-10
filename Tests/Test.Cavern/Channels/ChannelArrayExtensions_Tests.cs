using Cavern;
using Cavern.Channels;

namespace Test.Cavern.Channels;

/// <summary>
/// Tests for the <see cref="ChannelArrayExtensions"/> utility.
/// </summary>
[TestClass]
public class ChannelArrayExtensions_Tests {
    /// <summary>
    /// Tests that <see cref="ChannelArrayExtensions.GetSideChannels(Channel[])"/> returns correct counts for stereo.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSideChannels_Stereo_ReturnsOneAndOne() {
        Channel[] channels = [
            new Channel(0, -30),  // Left
            new Channel(0, 30)    // Right
        ];

        (int left, int right) = ChannelArrayExtensions.GetSideChannels(channels);

        Assert.AreEqual(1, left);
        Assert.AreEqual(1, right);
    }

    /// <summary>
    /// Tests that <see cref="ChannelArrayExtensions.GetSideChannels(Channel[])"/> returns correct counts for 5.1.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSideChannels_51Layout_ReturnsTwoAndTwo() {
        Channel[] channels = [
            new Channel(0, -30),   // Front Left
            new Channel(0, 30),    // Front Right
            new Channel(0, 0),     // Center (centerline, not counted)
            new Channel(15, 15, true), // LFE (excluded)
            new Channel(0, -110),  // Surround Left
            new Channel(0, 110)    // Surround Right
        ];

        (int left, int right) = ChannelArrayExtensions.GetSideChannels(channels);

        Assert.AreEqual(2, left);   // FL + SL
        Assert.AreEqual(2, right);  // FR + SR
    }

    /// <summary>
    /// Tests that <see cref="ChannelArrayExtensions.GetSideChannels(Channel[])"/> returns (1, 1) for only centerline channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSideChannels_OnlyCenterline_ReturnsOneAndOne() {
        Channel[] channels = [
            new Channel(0, 0),    // Front Center
        new Channel(0, 180)   // Rear Center
        ];

        (int left, int right) = ChannelArrayExtensions.GetSideChannels(channels);

        Assert.AreEqual(1, left);
        Assert.AreEqual(1, right);
    }

    /// <summary>
    /// Tests that <see cref="ChannelArrayExtensions.GetSideChannels(Channel[])"/> returns (1, 1) for empty/null array.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSideChannels_EmptyArray_ReturnsOneAndOne() {
        Channel[] channels = [];

        (int left, int right) = ChannelArrayExtensions.GetSideChannels(channels);

        Assert.AreEqual(1, left);
        Assert.AreEqual(1, right);
    }

    /// <summary>
    /// Tests that <see cref="ChannelArrayExtensions.GetSideChannels(Channel[])"/> returns (1, 1) for null array.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSideChannels_NullArray_ReturnsOneAndOne() {
        Channel[] channels = null;

        (int left, int right) = ChannelArrayExtensions.GetSideChannels(channels);

        Assert.AreEqual(1, left);
        Assert.AreEqual(1, right);
    }
}
