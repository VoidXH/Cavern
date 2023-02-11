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

        /// <summary>
        /// Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            base.Process(samples);
            for (int i = 0; i < samples.Length; ++i) {
                samples[i] = LowOutput[i] + HighOutput[i];
            }
        }

        /// <summary>
        /// Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            base.Process(samples, channel, channels);
            for (int sample = channel; sample < samples.Length; sample += channels) {
                samples[sample] = LowOutput[sample] + HighOutput[sample];
            }
        }
    }
}