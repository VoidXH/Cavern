using Cavern;

namespace Test.Cavern {
    /// <summary>
    /// Constant test values.
    /// </summary>
    static class Consts {
        /// <summary>
        /// Generic test value.
        /// </summary>
        internal const int nice = 69;

        /// <summary>
        /// Some samples used where audio samples are needed.
        /// </summary>
        internal static readonly float[] samples = { .1f, .2f, .3f, .4f, .5f };

        /// <summary>
        /// Some other samples used where different audio signals are needed.
        /// </summary>
        internal static readonly float[] samples2 = { .6f, .7f, .8f, .9f, 1 };

        /// <summary>
        /// The two sample arrays as a stereo signal for multichannel tests.
        /// </summary>
        internal static readonly MultichannelWaveform multichannel = new MultichannelWaveform(samples, samples2);
    }
}