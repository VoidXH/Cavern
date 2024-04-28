namespace Cavern.Filters {
    /// <summary>
    /// Used to showcase crossover distortion, this filter mixes crossover outputs.
    /// </summary>
    public class DebugCrossover : Crossover {
        /// <summary>
        /// Used to showcase 2nd-order crossover distortion, this filter mixes crossover outputs.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="frequency">Crossover frequency</param>
        public DebugCrossover(int sampleRate, double frequency) : base(sampleRate, frequency, 2) { }

        /// <summary>
        /// Used to showcase crossover distortion, this filter mixes crossover outputs.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="frequency">Crossover frequency</param>
        /// <param name="order">Number of filters per pass, 2 is recommended for mixing notch prevention</param>
        public DebugCrossover(int sampleRate, double frequency, int order) : base(sampleRate, frequency, order) { }

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            base.Process(samples);
            for (int i = 0; i < samples.Length; ++i) {
                samples[i] = LowOutput[i] + HighOutput[i];
            }
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            base.Process(samples, channel, channels);
            for (int sample = channel; sample < samples.Length; sample += channels) {
                samples[sample] = LowOutput[sample] + HighOutput[sample];
            }
        }
    }
}