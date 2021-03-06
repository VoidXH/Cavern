﻿namespace Cavern.Filters {
    /// <summary>Abstract audio filter.</summary>
    /// <remarks>You have to implement at least one signature of the Process function, otherwise they'll call each other.</remarks>
    public abstract class Filter {
        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public virtual void Process(float[] samples) => Process(samples, 0, 1);

        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public virtual void Process(float[] samples, int channel, int channels) {
            int channelSize = samples.Length / channels;
            float[] singleChannel = new float[channelSize];
            for (int sample = 0; sample < channelSize; ++sample)
                singleChannel[sample] = samples[sample * channels + channel];
            Process(singleChannel);
            for (int sample = 0; sample < channelSize; ++sample)
                samples[sample * channels + channel] = singleChannel[sample];
        }
    }
}