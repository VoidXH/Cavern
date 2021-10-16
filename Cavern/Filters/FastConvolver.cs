using Cavern.Utilities;
using System;

namespace Cavern.Filters {
    /// <summary>Performs an optimized convolution.</summary>
    /// <remarks>This filter adds delay, which is the smallest power of 2 larger than the length of the <see cref="Impulse"/>.</remarks>
    public class FastConvolver : Filter { // TODO: support 2 streams, the second is the imaginary input of the FFT
        /// <summary>Impulse response to convolve with.</summary>
        public float[] Impulse {
            get => impulse;
            set {
                impulse = value;
                transferFunction = new Complex[0];
            }
        }
        float[] impulse;

        /// <summary>Padded FFT of <see cref="Impulse"/>.</summary>
        Complex[] transferFunction = new Complex[0];
        /// <summary>FFT optimization.</summary>
        FFTCache cache;
        /// <summary>Cached samples for the next frame.</summary>
        float[] unprocessed;
        /// <summary>Buffer for samples that can be written to the output.</summary>
        float[] processed;
        /// <summary>Last handled sample in <see cref="unprocessed"/> and <see cref="processed"/>.</summary>
        int position;

        /// <summary>Construct a convolver for a target impulse response.</summary>
        public FastConvolver(float[] impulse) => this.impulse = impulse;

        /// <summary>Processes a ready set of the <see cref="unprocessed"/>.</summary>
        /// <remarks>Use the <see cref="unprocessed"/> array such as the next samples are in the first half only,
        /// the second half should be silent.</remarks>
        void ProcessFrame() {
            Complex[] result = Measurements.FFT(unprocessed, cache);
            for (int i = 0; i < result.Length; ++i)
                result[i].Multiply(ref transferFunction[i]);
            Measurements.InPlaceIFFT(result, cache);
            for (int i = 0; i < result.Length; ++i)
                processed[i] += result[i].Real;
        }

        /// <summary>Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] samples) {
            if (1 << QMath.Log2(samples.Length) != samples.Length)
                throw new ArgumentException("This filter only works with sample blocks the size of a power of 2.");

            int filterSize = 2 << QMath.Log2(Math.Max(samples.Length, impulse.Length) << 1);
            if (transferFunction.Length != filterSize) {
                cache = new FFTCache(filterSize);
                unprocessed = new float[filterSize];
                processed = new float[filterSize];
                position = 0;
                transferFunction = new Complex[filterSize];
                for (int i = 0; i < impulse.Length; ++i)
                    transferFunction[i].Real = impulse[i];
                Measurements.InPlaceFFT(transferFunction, cache);
            }

            // TODO: make this work with not just powers of 2, it's partially done
            int firstPass = Math.Min((filterSize >> 1) - position /* samples until frame processing */, samples.Length);
            Array.Copy(samples, 0, unprocessed, position, firstPass);
            Array.Copy(processed, position, samples, 0, firstPass);
            position += firstPass;
            if (position == filterSize >> 1) {
                Array.Copy(processed, position, processed, 0, position);
                Array.Clear(processed, position, position);
                ProcessFrame();
                position = samples.Length - firstPass;
                Array.Copy(samples, firstPass, unprocessed, 0, position);
                Array.Copy(processed, 0, samples, firstPass, position);
            }
        }
    }
}