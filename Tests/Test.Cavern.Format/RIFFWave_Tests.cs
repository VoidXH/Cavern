using Cavern.Format;

namespace Test.Cavern.Format {
    /// <summary>
    /// Tests the <see cref="RIFFWaveWriter"/> and <see cref="RIFFWaveReader"/> classes.
    /// </summary>
    [TestClass]
    public class RIFFWave_Tests {
        /// <summary>
        /// Tests if <see cref="RIFFWaveWriter.WriteBlock(float[], long, long)"/> creates a valid 8-bit integer WAV file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Int8() => TestBitDepth(BitDepth.Int8, 8192 * Consts.epsilon);

        /// <summary>
        /// Tests if <see cref="RIFFWaveWriter.WriteBlock(float[], long, long)"/> creates a valid 16-bit integer WAV file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Int16() => TestBitDepth(BitDepth.Int16, 32 * Consts.epsilon);

        /// <summary>
        /// Tests if <see cref="RIFFWaveWriter.WriteBlock(float[], long, long)"/> creates a valid 24-bit integer WAV file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Int24() => TestBitDepth(BitDepth.Int24, Consts.epsilon);

        /// <summary>
        /// Tests if <see cref="RIFFWaveWriter.WriteBlock(float[], long, long)"/> creates a valid 32-bit floating point WAV file.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Float32() => TestBitDepth(BitDepth.Float32, Consts.epsilon);

        /// <summary>
        /// Tests if <see cref="RIFFWaveWriter.WriteBlock(float[], long, long)"/> creates a valid WAV file with a given bit depth.
        /// </summary>
        static void TestBitDepth(BitDepth bits, float epsilon) {
            MemoryStream stream = new();
            RIFFWaveWriter writer = new(stream, 1, 4 * Consts.sampleRate, Consts.sampleRate, bits);
            RIFFWaveReader reader = new(stream);
            AudioReaderWriter_Tests.WriteBlockMono(writer, reader, epsilon);
            writer.Dispose();
            reader.Dispose();
        }
    }
}