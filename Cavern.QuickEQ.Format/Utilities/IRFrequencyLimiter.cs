using System;

using Cavern.Utilities;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Limits the frequency content of an impulse response to a target cutoff frequency.
    /// </summary>
    public class IRFrequencyLimiter {
        /// <summary>
        /// The sample rate of the input signal.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// The power-of-two factor by which the spectrum is cut and the sample rate divided.
        /// </summary>
        readonly int divisor;

        /// <summary>
        /// Construct a limiter for a given <paramref name="sampleRate"/> and <paramref name="cutoffFrequency"/>.
        /// The <paramref name="cutoffFrequency"/> must divide the <paramref name="sampleRate"/> by a power of 2.
        /// </summary>
        public IRFrequencyLimiter(int sampleRate, double cutoffFrequency) : this(sampleRate, cutoffFrequency, false) { }

        /// <summary>
        /// Construct a limiter for a given <paramref name="sampleRate"/> and <paramref name="cutoffFrequency"/>.
        /// The <paramref name="cutoffFrequency"/> must divide the <paramref name="sampleRate"/> by a power of 2, unless
        /// <paramref name="correctNonPowerOfTwo"/> is set, in which case the divisor is rounded down to the nearest power of two.
        /// </summary>
        public IRFrequencyLimiter(int sampleRate, double cutoffFrequency, bool correctNonPowerOfTwo) {
            Checks.ThrowIfNonPositive(sampleRate, nameof(sampleRate));
            Checks.ThrowIfNonPositive(cutoffFrequency, nameof(cutoffFrequency));
            if (cutoffFrequency <= 0 || cutoffFrequency * 2 > sampleRate) {
                throw new ArgumentOutOfRangeException(nameof(cutoffFrequency), "The cutoff frequency must be positive and at most half the sample rate.");
            }

            double ratio = sampleRate / (2 * cutoffFrequency);
            int finalDivisor = (int)Math.Floor(ratio);
            if (correctNonPowerOfTwo) {
                finalDivisor = QMath.Base2Floor(finalDivisor);
            } else if (finalDivisor < 1 || Math.Abs(ratio - finalDivisor) > 1e-9 || (finalDivisor & (finalDivisor - 1)) != 0) {
                throw new ArgumentException($"The sample rate must be divided by a power of two to reach the cutoff frequency, but it's {ratio}.");
            }
            this.sampleRate = sampleRate;
            divisor = finalDivisor;
        }

        /// <summary>
        /// Limit the frequency content of an impulse response. The input is FFT'd, the middle part is literally cut off so that
        /// only 1/divisor of the elements remain at each end (DC and Nyquist), then it's inverse FFT'd into a shorter impulse
        /// response at the divided sample rate.
        /// </summary>
        public float[] Process(float[] samples) {
            if (samples == null || samples.Length == 0) {
                throw new ArgumentException("The sample array must not be null or empty.", nameof(samples));
            }

            Complex[] fullSpectrum = samples.ParseForFFT().FFT();
            int outputLength = QMath.Base2Ceil(samples.Length / divisor),
                halfOutputLength = outputLength / 2;
            Complex[] cutSpectrum = new Complex[outputLength];
            Array.Copy(fullSpectrum, 0, cutSpectrum, 0, halfOutputLength);
            Array.Copy(fullSpectrum, fullSpectrum.Length - halfOutputLength, cutSpectrum, halfOutputLength, halfOutputLength);

            cutSpectrum.InPlaceIFFT();

            float[] result = new float[cutSpectrum.Length];
            for (int i = 0; i < result.Length; i++) {
                result[i] = cutSpectrum[i].Real;
            }
            return result;
        }

        /// <summary>
        /// Limit the frequency content of an impulse response and export it as a WAV file at the divided sample rate.
        /// </summary>
        public void Export(string path, float[] samples) => RIFFWaveWriter.Write(path, Process(samples), 1, sampleRate / divisor, BitDepth.Float32);
    }
}
