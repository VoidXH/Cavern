namespace Cavern.Filters {
    /// <summary>
    /// Simple convolution window
    /// </summary>
    public class SpikeConvolver : Convolver {
        /// <summary>Construct a spike convolver for a target impulse response.</summary>
        public SpikeConvolver(float[] impulse, int delay) : base(impulse, delay) {}

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] samples) {
            float[] convolved = new float[samples.Length + impulse.Length + delay];
            for (int step = 0; step < impulse.Length; ++step)
                if (impulse[step] != 0)
                    for (int sample = 0; sample < samples.Length; ++sample)
                        convolved[sample + step + delay] += samples[sample] * impulse[step];
            Finalize(samples, convolved);
        }
    }
}