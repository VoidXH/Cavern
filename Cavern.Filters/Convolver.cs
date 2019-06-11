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
            int delayedImpulse = impulse.Length + delay;
            float[] convolved = new float[samples.Length + delayedImpulse];
            for (int sample = 0; sample < samples.Length; ++sample)
                for (int step = 0, lastStep = impulse.Length; step < lastStep; ++step)
                    convolved[sample + step + delay] += samples[sample] * impulse[step];
            if (samples.Length > delayedImpulse) {
                // Drain cache
                Buffer.BlockCopy(convolved, 0, samples, 0, samples.Length * sizeof(float));
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    samples[sample] += future[sample];
                // Fill cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] = convolved[sample + samples.Length];
            } else {
                // Drain cache
                for (int sample = 0; sample < samples.Length; ++sample)
                    samples[sample] = convolved[sample] + future[sample];
                // Move cache
                int futureEnd = delayedImpulse - samples.Length;
                for (int sample = 0; sample < futureEnd; ++sample)
                    future[sample] = future[sample + samples.Length];
                Array.Clear(future, futureEnd, samples.Length);
                // Merge cache
                for (int sample = 0; sample < delayedImpulse; ++sample)
                    future[sample] += convolved[sample + samples.Length];
            }
        }
    }
}