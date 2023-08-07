namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Constant test values.
    /// </summary>
    static class Consts {
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
    }
}