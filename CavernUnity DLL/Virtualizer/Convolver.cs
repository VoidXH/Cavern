using Cavern.QuickEQ;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>Simple convolution window filter.</summary>
    public class Convolver {
        /// <summary>Impulse response to convolve.</summary>
        readonly float[] Impulse;
        /// <summary>FFT of <see cref="Impulse"/>.</summary>
        readonly Complex[] ImpulseFFT;
        /// <summary>I like Borderlands 2.</summary>
        readonly FFTCache Fasterify;
        /// <summary>Samples to be copied to the beginning of the next output.</summary>
        readonly float[] Future;
        /// <summary>Size of <see cref="Impulse"/>.</summary>
        readonly int ImpulseLength;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public Convolver(float[] Impulse) {
            this.Impulse = Impulse;
            ImpulseLength = Impulse.Length;
            Fasterify = new FFTCache(ImpulseLength);
            ImpulseFFT = Measurements.FFT(Impulse, Fasterify);
            Future = new float[ImpulseLength];
        }

        /// <summary>Apply convolution on a set of samples.</summary>
        public void Process(float[] Samples) {
            // Actual convolution
            int SampleCount = Samples.Length;
            float[] Convolved = new float[SampleCount + ImpulseLength];
            for (int Sample = 0; Sample < SampleCount; ++Sample)
                for (int Step = 0; Step < ImpulseLength; ++Step)
                    Convolved[Sample + Step] += Samples[Sample] * Impulse[Step];
            if (SampleCount > ImpulseLength) {
                // Drain cache
                for (int Sample = 0; Sample < SampleCount; ++Sample)
                    Samples[Sample] = Convolved[Sample];
                for (int Sample = 0; Sample < ImpulseLength; ++Sample)
                    Samples[Sample] += Future[Sample];
                // Fill cache
                for (int Sample = 0; Sample < ImpulseLength; ++Sample)
                    Future[Sample] = Convolved[Sample + SampleCount];
            } else {
                // Drain cache
                for (int Sample = 0; Sample < SampleCount; ++Sample)
                    Samples[Sample] = Convolved[Sample] + Future[Sample];
                // Move cache
                int FutureEnd = ImpulseLength - SampleCount;
                for (int Sample = 0; Sample < FutureEnd; ++Sample)
                    Future[Sample] = Future[Sample + SampleCount];
                for (int Sample = FutureEnd; Sample < ImpulseLength; ++Sample)
                    Future[Sample] = 0;
                // Merge cache
                for (int Sample = 0; Sample < ImpulseLength; ++Sample)
                    Future[Sample] += Convolved[Sample + SampleCount];
            }
        }
    }
}