using System;

namespace Cavern.Filters {
    /// <summary>Simple convolution window filter.</summary>
    public class Convolver : Filter {
        /// <summary>Impulse response to convolve with.</summary>
        float[] Impulse;
        /// <summary>Samples to be copied to the beginning of the next output.</summary>
        float[] Future;
        /// <summary>Additional impulse delay in samples.</summary>
        readonly int Delay;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public Convolver(float[] Impulse, int Delay) {
            this.Impulse = Impulse;
            this.Delay = Delay;
            Future = new float[Impulse.Length + Delay];
        }

        /// <summary>Change the impulse response to convolve with. If the length differs from the old impulse, the cached future samples will be dropped.</summary>
        public void ReplaceImpulse(float[] NewImpulse) {
            if (Impulse.Length != NewImpulse.Length)
                Future = new float[NewImpulse.Length + Delay];
            Impulse = NewImpulse;
        }

        /// <summary>Apply convolution on a set of samples.</summary>
        public override void Process(float[] Samples) {
            // Actual convolution
            int SampleCount = Samples.Length, DelayedImpulse = Impulse.Length + Delay;
            float[] Convolved = new float[SampleCount + DelayedImpulse];
            for (int Sample = 0; Sample < SampleCount; ++Sample)
                for (int Step = 0, LastStep = Impulse.Length; Step < LastStep; ++Step)
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