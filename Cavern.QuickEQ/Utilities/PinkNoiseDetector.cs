using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Detects pink noise in sequential blocks of samples.
    /// </summary>
    public class PinkNoiseDetector {
        /// <summary>
        /// Number of frames to calculate the probability from. The more frames measured, the better the chance of true positives,
        /// but this decreases detection speed.
        /// </summary>
        public int Adaptation {
            get => history.Length;
            set {
                if (value < history.Length) {
                    Array.Copy(history, history.Length - value, history, 0, value);
                    Array.Resize(ref history, value);
                } else if (value > history.Length) {
                    float[] old = history;
                    history = new float[value];
                    Array.Copy(old, 0, history, value - old.Length, old.Length);
                }
            }
        }

        /// <summary>
        /// Minimum noise detection frequency.
        /// </summary>
        public float MinFreq {
            get => startIndex * nyquistFrequency / (float)reference.Length;
            set => startIndex = (int)(value * reference.Length / nyquistFrequency + 1);
        }

        /// <summary>
        /// Maximum noise detection frequency.
        /// </summary>
        public float MaxFreq {
            get => endIndex * nyquistFrequency / (float)reference.Length;
            set => endIndex = (int)(value * reference.Length / nyquistFrequency + 1);
        }

        /// <summary>
        /// Power spectral density of pink noise for the used block size.
        /// </summary>
        /// <remarks>It will be half the block size, as FFT is symmetric.</remarks>
        readonly float[] reference;

        /// <summary>
        /// Half sample rate of the continuous signal that arrives block by block.
        /// </summary>
        readonly int nyquistFrequency;

        /// <summary>
        /// FFT preallocation for performance.
        /// </summary>
        FFTCache cache;

        /// <summary>
        /// The probabilities of the last <see cref="Adaptation"/> frames.
        /// </summary>
        float[] history = new float[5];

        /// <summary>
        /// Index of the minimum frequency band in <see cref="reference"/>.
        /// </summary>
        int startIndex;

        /// <summary>
        /// Index of the maximum frequency band in <see cref="reference"/>.
        /// </summary>
        int endIndex;

        /// <summary>
        /// Detects pink noise in sequential blocks of samples.
        /// </summary>
        /// <param name="blockSize">Size of the sequential audio blocks that will be supplied to
        /// <see cref="GetProbability(float[])"/></param>
        /// <param name="sampleRate">Sample rate of the continuous signal that arrives block by block</param>
        /// <param name="cache">FFT preallocation for performance</param>
        /// <param name="minFreq">Minimum noise detection frequency</param>
        /// <param name="maxFreq">Maximum noise detection frequency</param>
        /// <remarks>All sample blocks have to match the block size set here. The block size has to be a power of 2 for FFT.</remarks>
        public PinkNoiseDetector(int blockSize, int sampleRate, FFTCache cache = null, float minFreq = 100, float maxFreq = 16000) {
            reference = new float[blockSize >> 1];
            for (int i = 0; i < reference.Length;) {
                reference[i] = .01f / ++i; // Pink noise spectrum starts at -20 dB
            }
            nyquistFrequency = sampleRate >> 1;
            this.cache = cache;

            float alignment = nyquistFrequency / (float)reference.Length; // Bandwidth of a bin
            startIndex = (int)(minFreq / alignment + 1);
            endIndex = (int)(maxFreq / alignment + 1);
        }

        /// <summary>
        /// Get the likeliness that the next block is pink noise.
        /// </summary>
        public float GetProbability(float[] sampleBlock) =>
            GetProbabilityOfSpectrum(sampleBlock.FFT1D(cache ??= new FFTCache(sampleBlock.Length)));

        /// <summary>
        /// Get the likeliness that the next block (in the form of |FFT|) is pink noise.
        /// </summary>
        public float GetProbabilityOfSpectrum(float[] spectrum) {
            float rmsError = 0;
            for (int i = startIndex; i < endIndex; i++) {
                float val = (spectrum[i] * spectrum[i] - reference[i]);
                rmsError += val * val;
            }
            rmsError = MathF.Sqrt(rmsError / (endIndex - startIndex));

            Array.Copy(history, 1, history, 0, history.Length - 1);
            history[^1] = 1 / (1 + rmsError);
            return QMath.Average(history);
        }
    }
}