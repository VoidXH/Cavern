using Cavern.Format;

namespace Test.Cavern.Format {
    /// <summary>
    /// Common tests for descendants of <see cref="AudioReader"/> and <see cref="AudioWriter"/>.
    /// </summary>
    static class AudioReaderWriter_Tests {
        /// <summary>
        /// Tests writing a 4-second mono track with the interlaced writer to a format and reading it back.
        /// An error margin of <paramref name="epsilon"/> might increase for lower bit depths.
        /// </summary>
        public static void WriteBlockMono(AudioWriter writer, AudioReader reader, float epsilon) {
            float[] samples = AudioSamples.Sweep4Sec;
            writer.WriteHeader();
            writer.WriteBlock(samples, 0, samples.Length);
            float[] result = reader.Read();
            CompareWaveforms(samples, result, epsilon);
        }

        /// <summary>
        /// Tests writing a 4-second stereo track with the multichannel writer to a format and reading it back.
        /// </summary>
        public static void WriteBlockStereo(AudioWriter writer, AudioReader reader) {
            float[][] samples = AudioSamples.Sweep4SecStereo;
            writer.WriteHeader();
            writer.WriteBlock(samples, 0, samples[0].Length);
            float[][] result = reader.ReadMultichannel();
            CollectionAssert.AreEqual(samples[0], result[0]);
            CollectionAssert.AreEqual(samples[1], result[1]);
        }

        /// <summary>
        /// Check if two waveforms are similar enough in an error margin.
        /// </summary>
        public static void CompareWaveforms(float[] lhs, float[] rhs, float epsilon) {
            for (int i = 0; i < lhs.Length; i++) {
                if (Math.Abs(lhs[i] - rhs[i]) > epsilon) {
                    Assert.Fail($"[{i}]: |{lhs[i]} - {rhs[i]}| = {Math.Abs(lhs[i] - rhs[i])} > {epsilon}");
                }
            }
        }
    }
}