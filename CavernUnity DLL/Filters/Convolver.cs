using System;

namespace Cavern.Filters {
    /// <summary>Simple convolution window filter.</summary>
    public class Convolver : Filter {
        /// <summary>Additional impulse delay in samples.</summary>
        public int Delay {
            get => _Delay;
            set => Future = new float[_Impulse.Length + (_Delay = value)];
        }
        /// <summary>Impulse response to convolve with.</summary>
        public float[] Impulse {
            get => _Impulse;
            set {
                if (_Impulse.Length != value.Length)
                    Future = new float[value.Length + _Delay];
                _Impulse = value;
            }
        }

        /// <summary>Additional impulse delay in samples.</summary>
        int _Delay;
        /// <summary>Impulse response to convolve with.</summary>
        float[] _Impulse;
        /// <summary>Samples to be copied to the beginning of the next output.</summary>
        float[] Future;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public Convolver(float[] Impulse, int Delay) {
            _Impulse = Impulse;
            this.Delay = Delay;
        }

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] Samples) {
            // Actual convolution
            int SampleCount = Samples.Length, DelayedImpulse = _Impulse.Length + _Delay;
            float[] Convolved = new float[SampleCount + DelayedImpulse];
            for (int Sample = 0; Sample < SampleCount; ++Sample)
                for (int Step = 0, LastStep = _Impulse.Length; Step < LastStep; ++Step)
                    Convolved[Sample + Step + _Delay] += Samples[Sample] * _Impulse[Step];
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

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public override void Process(float[] Samples, int Channel, int Channels) => throw new NotImplementedException(); // TODO
    }
}