using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Gets interlaced samples and converts them to multichannel, block by block.
    /// </summary>
    public class RunningChannelSeparator {
        /// <summary>
        /// Function that fills the <see cref="input"/> sample array.
        /// </summary>
        public Action<float[]> GetSamples;

        /// <summary>
        /// Input cache array.
        /// </summary>
        float[] input = new float[0];

        /// <summary>
        /// Output cache array.
        /// </summary>
        readonly float[][] output;

        /// <summary>
        /// Gets interlaced samples and converts them to multichannel, block by block.
        /// </summary>
        public RunningChannelSeparator(int channels) {
            output = new float[channels][];
            output[0] = new float[0];
        }

        /// <summary>
        /// Get a new multichannel block of <paramref name="samples"/>.
        /// </summary>
        public float[][] Update(int samples) {
            if (output[0].Length != samples) {
                input = new float[output.Length * samples];
                for (int i = 0; i < output.Length; i++) {
                    output[i] = new float[samples];
                }
            }

            GetSamples(input);
            WaveformUtils.InterlacedToMultichannel(input, output);
            return output;
        }
    }
}