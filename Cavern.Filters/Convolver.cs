using System;

namespace Cavern.Filters {
    /// <summary>Simple convolution window filter.</summary>
    public class Convolver : Filter {
        /// <summary>Additional impulse delay in samples.</summary>
        public int Delay {
            get => delay;
            set => future = new float[impulse.Length + (delay = value)];
        }
        /// <summary>Impulse response to convolve with.</summary>
        public float[] Impulse {
            get => impulse;
            set {
                if (future.Length != (impulse = value).Length)
                    future = new float[value.Length + delay];
            }
        }

        /// <summary>Additional impulse delay in samples.</summary>
        int delay;
        /// <summary>Impulse response to convolve with.</summary>
        float[] impulse;
        /// <summary>Samples to be copied to the beginning of the next output.</summary>
        float[] future;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public Convolver(float[] impulse, int delay) {
            this.impulse = (float[])impulse.Clone();
            Delay = delay;
        }

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] samples) {
            // Actual convolution
            int sampleCount = samples.Length, delayedImpulse = impulse.Length + delay;
            float[] convolved = new float[sampleCount + delayedImpulse];
            for (int sample = 0; sample < sampleCount; ++sample)
                for (int step = 0, lastStep = impulse.Length; step < lastStep; ++step)
                    convolved[sample + step + delay] += samples[sample] * impulse[step];
            if (sampleCount > delayedImpulse) {
                // Drain cache
                Buffer.BlockCopy(convolved, 0, samples, 0, sampleCount * sizeof(float));
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    samples[sample] += future[sample];
                // Fill cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] = convolved[sample + sampleCount];
            } else {
                // Drain cache
                for (int sample = 0; sample < sampleCount; ++sample)
                    samples[sample] = convolved[sample] + future[sample];
                // Move cache
                int futureEnd = delayedImpulse - sampleCount;
                for (int sample = 0; sample < futureEnd; ++sample)
                    future[sample] = future[sample + sampleCount];
                Array.Clear(future, futureEnd, sampleCount);
                // Merge cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] += convolved[sample + sampleCount];
            }
        }

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            // Actual convolution
            int sampleCount = samples.Length, delayedImpulse = impulse.Length + delay;
            float[] convolved = new float[sampleCount + delayedImpulse];
            for (int sample = 0; sample < sampleCount; ++sample)
                for (int step = 0, LastStep = impulse.Length; step < LastStep; ++step)
                    convolved[sample + step + delay] += samples[sample * channels + channel] * impulse[step];
            if (sampleCount > delayedImpulse) {
                // Drain cache
                Buffer.BlockCopy(convolved, 0, samples, 0, sampleCount * sizeof(float));
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    samples[sample * channels + channel] += future[sample];
                // Fill cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] = convolved[sample + sampleCount];
            } else {
                // Drain cache
                for (int sample = 0; sample < sampleCount; ++sample)
                    samples[sample * channels + channel] = convolved[sample] + future[sample];
                // Move cache
                int futureEnd = delayedImpulse - sampleCount;
                for (int sample = 0; sample < futureEnd; ++sample)
                    future[sample] = future[sample + sampleCount];
                Array.Clear(future, futureEnd, sampleCount);
                // Merge cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] += convolved[sample + sampleCount];
            }
        }
    }
}