using System;

namespace Cavern.Virtualizer {
    /// <summary>Simple convolution window filter.</summary>
    public class Convolver {
        /// <summary>Impulse response to convolve.</summary>
        readonly float[] Impulse;
        /// <summary>Samples to be copied to the beginning of the next output.</summary>
        readonly float[] Future;
        /// <summary>Size of <see cref="Impulse"/>.</summary>
        readonly int ImpulseLength;
        /// <summary>Additional impulse delay in samples.</summary>
        readonly int Delay;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public Convolver(float[] Impulse, int Delay) {
            this.Impulse = Impulse;
            ImpulseLength = Impulse.Length;
            this.Delay = Delay;
            Future = new float[ImpulseLength + Delay];
        }

        /// <summary>Apply convolution on a set of samples.</summary>
        public void Process(float[] Samples) {
            // Actual convolution
            int SampleCount = Samples.Length, DelayedImpulse = ImpulseLength + Delay;
            float[] Convolved = new float[SampleCount + DelayedImpulse];
            for (int Sample = 0; Sample < SampleCount; ++Sample)
                for (int Step = 0; Step < ImpulseLength; ++Step)
                    Convolved[Sample + Step + Delay] += Samples[Sample] * Impulse[Step];
            if (SampleCount > DelayedImpulse) {
                // Drain cache
                Buffer.BlockCopy(Convolved, 0, Samples, 0, SampleCount * sizeof(float));
                for (int Sample = 0; Sample < DelayedImpulse; ++Sample)
                    Samples[Sample] += Future[Sample];
                // Fill cache
                for (int Sample = 0; Sample < DelayedImpulse; ++Sample)
                    Future[Sample] = Convolved[Sample + SampleCount];
            } else {
                // Drain cache
                for (int Sample = 0; Sample < SampleCount; ++Sample)
                    Samples[Sample] = Convolved[Sample] + Future[Sample];
                // Move cache
                int FutureEnd = DelayedImpulse - SampleCount;
                for (int Sample = 0; Sample < FutureEnd; ++Sample)
                    Future[Sample] = Future[Sample + SampleCount];
                Array.Clear(Future, FutureEnd, SampleCount);
                // Merge cache
                for (int Sample = 0; Sample < DelayedImpulse; ++Sample)
                    Future[Sample] += Convolved[Sample + SampleCount];
            }
        }
    }
}