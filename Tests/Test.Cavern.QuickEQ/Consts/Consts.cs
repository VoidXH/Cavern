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
    /// Equalizer with a 400 Hz wide triangle peak of 6 dB at 500 Hz.
    /// </summary>
    internal static readonly Equalizer peakAt500Hz = new Equalizer([
        new(300, 0), new(500, 6), new(700, 0)
    ], true);
}
