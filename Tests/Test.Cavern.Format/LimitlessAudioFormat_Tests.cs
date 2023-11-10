using Cavern.Format;

namespace Test.Cavern.Format {
    /// <summary>
    /// Tests the <see cref="LimitlessAudioFormatWriter"/> and <see cref="LimitlessAudioFormatReader"/> classes.
    /// </summary>
    [TestClass]
    public class LimitlessAudioFormat_Tests {
        /// <summary>
        /// Tests if the interlaced <see cref="LimitlessAudioFormatWriter.WriteBlock(float[], long, long)"/> creates a valid LAF file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Mono() {
            MemoryStream stream = new();
            using LimitlessAudioFormatWriter writer = new(stream, 4 * Consts.sampleRate, Consts.sampleRate, BitDepth.Float32, Consts.mono);
            using LimitlessAudioFormatReader reader = new(stream);
            AudioReaderWriter_Tests.WriteBlockMono(writer, reader, Consts.epsilon);
        }

        /// <summary>
        /// Tests if the multichannel <see cref="LimitlessAudioFormatWriter.WriteBlock(float[][], long, long)"/> creates a valid LAF file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Stereo() {
            MemoryStream stream = new();
            using LimitlessAudioFormatWriter writer = new(stream, 4 * Consts.sampleRate, Consts.sampleRate, BitDepth.Float32, Consts.stereo);
            using LimitlessAudioFormatReader reader = new(stream);
            AudioReaderWriter_Tests.WriteBlockStereo(writer, reader);
        }
    }
}