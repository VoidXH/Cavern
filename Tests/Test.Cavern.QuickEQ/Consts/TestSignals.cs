using Cavern;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ.Consts;

/// <summary>
/// Precomputed signals to optimize test run times by reuse.
/// </summary>
public static class TestSignals {
    /// <summary>
    /// Transfer function of a 440 Hz sine wave (256 samples).
    /// </summary>
    public static readonly Complex[] sine440TF = Measurements.FFT(WaveformGenerator.Sine(440, 256, Listener.DefaultSampleRate));
}
