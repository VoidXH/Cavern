using System.Runtime.CompilerServices;

namespace Cavern.Filters {
    /// <summary>
    /// Simple convolution window
    /// </summary>
    public class SpikeConvolver : Convolver {
        /// <summary>
        /// Construct a spike convolver for a target impulse response.
        /// </summary>
        /// <param name="impulse">Impulse response to convolve with</param>
        /// <param name="delay">Additional impulse delay in samples</param>
        public SpikeConvolver(float[] impulse, int delay) : base(impulse, delay) {}

        /// <summary>
        /// Perform a convolution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] SpikeConvolve(float[] impulse, float[] samples) {
            float[] convolved = new float[impulse.Length + samples.Length];
            for (int step = 0; step < impulse.Length; ++step) {
                if (impulse[step] != 0) {
                    for (int sample = 0; sample < samples.Length; ++sample) {
                        convolved[step + sample] += impulse[step] * samples[sample];
                    }
                }
            }
            return convolved;
        }

        /// <summary>
        /// Perform a convolution with a delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] SpikeConvolve(float[] impulse, float[] samples, int delay) {
            float[] convolved = new float[impulse.Length + samples.Length + delay];
            for (int step = 0; step < impulse.Length; ++step) {
                if (impulse[step] != 0) {
                    for (int sample = 0; sample < samples.Length; ++sample) {
                        convolved[step + sample + delay] += impulse[step] * samples[sample];
                    }
                }
            }
            return convolved;
        }

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            float[] convolved;
            if (delay == 0) {
                convolved = SpikeConvolve(impulse, samples);
            } else {
                convolved = SpikeConvolve(impulse, samples, delay);
            }
            Finalize(samples, convolved);
        }
    }
}