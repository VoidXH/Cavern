using Cavern.QuickEQ.Equalization;

namespace Test.Cavern.QuickEQ.Consts;

/// <summary>
/// Constant test values.
/// </summary>
static class Constants {
    /// <summary>
    /// Test sample rate.
    /// </summary>
    internal const int sampleRate = 48000;

    /// <summary>
    /// Convolution length used for tests.
    /// </summary>
    internal const int convolutionLength = 4096;

    /// <summary>
    /// Allowed floating point margin of error.
    /// </summary>
    internal const float delta = .000001f;

    /// <summary>
    /// Path to the test data folder, relative to the launch folder.
    /// </summary>
    internal static readonly string testData = Path.Combine("..", "..", "TestData");

    /// <summary>
    /// A full range speaker measurement in impulse response format.
    /// </summary>
    internal static readonly string fullRange1 = Path.Combine(testData, "IRs", "FL IR 1.wav");

    /// <summary>
    /// Equalizer with a 400 Hz wide triangle peak of 6 dB at 500 Hz.
    /// </summary>
    internal static readonly Equalizer peakAt500Hz = new Equalizer([
        new(300, 0), new(500, 6), new(700, 0)
    ], true);
}
