using System;

namespace Cavern.Filters {
    /// <summary>Signal level multiplier filter.</summary>
    public class Gain : Filter {
        /// <summary>Filter gain in decibels.</summary>
        public double GainValue { get; set; }

        /// <summary>Signal level multiplier filter.</summary>
        /// <param name="gain">Filter gain in decibels</param>
        public Gain(double gain) => GainValue = gain;

        /// <summary>Apply gain on an array of samples. This filter can be used on multiple streams.</summary>
        public override void Process(float[] samples) {
            float vGain = (float)Math.Pow(10, GainValue * .05);
            for (int sample = 0; sample < samples.Length; ++sample)
                samples[sample] *= vGain;
        }

        /// <summary>Apply gain on an array of samples. This filter can be used on multiple streams.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            float vGain = (float)Math.Pow(10, GainValue * .05);
            for (int sample = channel; sample < samples.Length; sample += channels)
                samples[sample] *= vGain;
        }
    }
}