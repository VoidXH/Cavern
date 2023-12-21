using Cavern;
using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Constant test values.
    /// </summary>
    static class Consts {
        /// <summary>
        /// Allowed floating point margin of error.
        /// </summary>
        internal const float delta = .000001f;

        /// <summary>
        /// Generic test value.
        /// </summary>
        internal const int nice = 69;

        /// <summary>
        /// Some samples used where audio samples are needed.
        /// </summary>
        internal static readonly float[] samples = [.1f, .2f, .3f, .4f, .5f];

        /// <summary>
        /// Some other samples used where different audio signals are needed.
        /// </summary>
        internal static readonly float[] samples2 = [.6f, .7f, .8f, .9f, 1];

        /// <summary>
        /// A 4-sample interlaced stereo signal for stereo tests.
        /// </summary>
        internal static readonly float[] stereoSamples = [.1f, .1f, 0, .2f, .1f, .3f, .1f, .5f];

        /// <summary>
        /// The result of convolving <see cref="samples"/> with <see cref="samples2"/>.
        /// </summary>
        internal static readonly float[] convolved = [.06f, .19f, .4f, .7f, 1.1f, 1.14f, 1.06f, .85f, .5f, 0];

        /// <summary>
        /// Some complex samples used where complex samples are needed.
        /// </summary>
        internal static readonly Complex[] complexSamples = [
            new Complex(.1f, .6f), new Complex(.2f, .7f), new Complex(.3f, .8f), new Complex(.4f, .9f), new Complex(.5f, 1)
        ];

        /// <summary>
        /// The two sample arrays as a stereo signal for multichannel tests.
        /// </summary>
        internal static readonly MultichannelWaveform multichannel = new MultichannelWaveform(samples, samples2);
    }
}