using Cavern;

namespace Test.Cavern.Format {
    /// <summary>
    /// Constant test values.
    /// </summary>
    static class Consts {
        /// <summary>
        /// Test sample rate.
        /// </summary>
        internal const int sampleRate = 48000;

        /// <summary>
        /// Allowed floating point margin of error.
        /// </summary>
        internal const float epsilon = .000001f;

        /// <summary>
        /// Mono channel layout.
        /// </summary>
        internal static readonly Channel[] mono = new[] { new Channel(0, 0) };

        /// <summary>
        /// Stereo channel layout.
        /// </summary>
        internal static readonly Channel[] stereo = new[] { new Channel(0, -45), new Channel(0, 45) };
    }
}