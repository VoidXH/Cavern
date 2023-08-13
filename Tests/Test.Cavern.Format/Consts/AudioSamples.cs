using Cavern.QuickEQ.SignalGeneration;

namespace Test.Cavern.Format {
    /// <summary>
    /// Pre-rendered audio samples for reuse in tests.
    /// </summary>
    static class AudioSamples {
        /// <summary>
        /// 4 seconds of linear sweep from 20 Hz to 20 kHz at a 48 kHz sampling rate.
        /// </summary>
        public static float[] Sweep4Sec => sweep4Sec ??= SweepGenerator.Linear(20, 20000, 4 * Consts.sampleRate, Consts.sampleRate);
        static float[] sweep4Sec;

        /// <summary>
        /// 4 seconds of a linear and an exponential sweep from 20 Hz to 20 kHz at a 48 kHz sampling rate.
        /// </summary>
        public static float[][] Sweep4SecStereo => sweep4SecStereo ??= new[]
        { Sweep4Sec, SweepGenerator.Exponential(20, 20000, 4 * Consts.sampleRate, Consts.sampleRate) };
        static float[][] sweep4SecStereo;
    }
}