using System;

namespace Cavern.Filters {
    /// <summary>
    /// Signal level multiplier filter.
    /// </summary>
    public class Gain : Filter {
        /// <summary>
        /// Filter gain in decibels.
        /// </summary>
        public double GainValue {
            get => 20 * Math.Log10(gainValue);
            set => gainValue = (float)Math.Pow(10, value * .05);
        }

        /// <summary>
        /// Filter gain as a multiplier.
        /// </summary>
        float gainValue;

        /// <summary>
        /// Signal level multiplier filter.
        /// </summary>
        /// <param name="gain">Filter gain in decibels</param>
        public Gain(double gain) => GainValue = gain;

        /// <summary>
        /// Apply gain on an array of samples. This filter can be used on multiple streams.
        /// </summary>
        public override void Process(float[] samples) {
            for (int sample = 0; sample < samples.Length; ++sample) {
                samples[sample] *= gainValue;
            }
        }

        /// <summary>
        /// Apply gain on an array of samples. This filter can be used on multiple streams.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            for (int sample = channel; sample < samples.Length; sample += channels) {
                samples[sample] *= gainValue;
            }
        }
    }
}