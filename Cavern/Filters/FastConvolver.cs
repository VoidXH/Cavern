using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs an optimized convolution.
    /// </summary>
    /// <remarks>This filter adds delay, which is the smallest power of 2 larger than the length of the <see cref="Impulse"/>.</remarks>
    public class FastConvolver : Filter {
        /// <summary>
        /// Impulse response to convolve with.
        /// </summary>
        public float[] Impulse {
            get => impulse;
            set {
                if (impulse.Length != value.Length) {
                    int filterSize = 2 << QMath.Log2(value.Length << 1) << shiftFactor;
                    transferFunction = new Complex[filterSize];
                    cache = new FFTCache(filterSize);
                    unprocessed = new float[filterSize];
                    processed = new float[filterSize];
                    position = 0;
                }
                impulse = value;
                for (int i = 0; i < impulse.Length; ++i)
                    transferFunction[i].Real = impulse[i];
                Array.Clear(transferFunction, impulse.Length, transferFunction.Length - impulse.Length);
                transferFunction.InPlaceFFT(cache);
            }
        }
        float[] impulse = new float[0];

        /// <summary>
        /// FFT size shift. This increases delay, decreases CPU use.
        /// </summary>
        readonly int shiftFactor;

        /// <summary>
        /// Padded FFT of <see cref="Impulse"/>.
        /// </summary>
        Complex[] transferFunction;

        /// <summary>
        /// FFT optimization.
        /// </summary>
        FFTCache cache;

        /// <summary>
        /// Cached samples for the next frame.
        /// </summary>
        float[] unprocessed;

        /// <summary>
        /// Buffer for samples that can be written to the output.
        /// </summary>
        float[] processed;

        /// <summary>
        /// Last handled sample in <see cref="unprocessed"/> and <see cref="processed"/>.
        /// </summary>
        int position;

        /// <summary>
        /// Construct a convolver for a target impulse response.
        /// </summary>
        public FastConvolver(float[] impulse) => Impulse = impulse;

        /// <summary>
        /// Construct a convolver for a target impulse response with increased FFT factor.
        /// </summary>
        public FastConvolver(float[] impulse, int shiftFactor) {
            this.shiftFactor = shiftFactor;
            Impulse = impulse;
        }

        /// <summary>
        /// Processes a ready set of the <see cref="unprocessed"/>.
        /// </summary>
        /// <remarks>Use the <see cref="unprocessed"/> array such as the next samples are in the first half only,
        /// the second half should be silent.</remarks>
        void ProcessFrame() {
            Complex[] result = unprocessed.FFT(cache);
            result.Convolve(transferFunction);
            result.InPlaceIFFT(cache);
            for (int i = 0; i < result.Length; ++i)
                processed[i] += result[i].Real;
        }

        /// <summary>
        /// Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            for (int sample = 0, step = processed.Length >> 1; sample < samples.Length; sample += step) {
                int remainingSamples = Math.Min(step, samples.Length - sample),
                    firstPass = Math.Min(step - position /* samples until frame processing */, remainingSamples);
                Array.Copy(samples, sample, unprocessed, position, firstPass);
                Array.Copy(processed, position, samples, sample, firstPass);
                position += firstPass;
                if (position == step) {
                    Array.Copy(processed, position, processed, 0, step);
                    Array.Clear(processed, position, step);
                    ProcessFrame();
                    position = remainingSamples - firstPass;
                    Array.Copy(samples, sample + firstPass, unprocessed, 0, position);
                    Array.Copy(processed, 0, samples, sample + firstPass, position);
                }
            }
        }

        /// <summary>
        /// Performs the convolution of two real signals. The FFT of the result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/> to match in a length of a power of 2.</remarks>
        public static Complex[] ConvolveFourier(float[] excitation, float[] impulse, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(excitation.Length);
            Complex[] excitationFFT = excitation.FFT(cache);
            excitationFFT.Convolve(impulse.FFT(cache));
            return excitationFFT;
        }

        /// <summary>
        /// Performs the convolution of two real signals. The real result is returned.
        /// </summary>
        /// <remarks>Requires <paramref name="excitation"/> and <paramref name="impulse"/> to match in a length of a power of 2.</remarks>
        public static Complex[] Convolve(float[] excitation, float[] impulse, FFTCache cache = null) {
            Complex[] result = ConvolveFourier(excitation, impulse, cache);
            result.InPlaceIFFT();
            return result;
        }
    }
}