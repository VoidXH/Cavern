using System;

namespace Cavern.Filters {
    /// <summary>
    /// Delays the audio.
    /// </summary>
    public class Delay : Filter {
        /// <summary>
        /// Delay in samples.
        /// </summary>
        public int DelaySamples {
            get => cache[0].Length;
            set {
                if (cache[0].Length != value) {
                    RecreateCaches(value);
                }
            }
        }

        /// <summary>
        /// Cached samples for the next block. Alternates between two arrays to prevent memory allocation.
        /// </summary>
        readonly float[][] cache = new float[2][];

        /// <summary>
        /// The used cache (0 or 1).
        /// </summary>
        int usedCache;

        void RecreateCaches(int size) {
            cache[0] = new float[size];
            cache[1] = new float[size];
        }

        /// <summary>
        /// Create a delay for a given length in samples.
        /// </summary>
        public Delay(int samples) => RecreateCaches(samples);

        /// <summary>
        /// Create a delay for a given length in seconds.
        /// </summary>
        public Delay(double time, int sampleRate) => RecreateCaches((int)(time * sampleRate + .5f));

        /// <summary>
        /// Apply delay on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            int delaySamples = cache[0].Length;
            float[] cacheToFill = cache[1 - usedCache], cacheToDrain = cache[usedCache];
            // Sample array can hold the cache
            if (delaySamples <= samples.Length) {
                // Fill cache
                Array.Copy(samples, samples.Length - delaySamples, cacheToFill, 0, delaySamples);
                // Move self
                for (int sample = samples.Length - 1; sample >= delaySamples; --sample) {
                    samples[sample] = samples[sample - delaySamples];
                }
                // Drain cache
                Array.Copy(cacheToDrain, samples, delaySamples);
                usedCache = 1 - usedCache; // Switch caches
            }
            // Cache can hold the sample array
            else {
                // Fill cache
                Array.Copy(samples, cacheToFill, samples.Length);
                // Drain cache
                Array.Copy(cacheToDrain, samples, samples.Length);
                // Move cache
                Array.Copy(cacheToDrain, samples.Length, cacheToDrain, 0, delaySamples - samples.Length);
                // Combine cache
                Array.Copy(cacheToFill, 0, cacheToDrain, delaySamples - samples.Length, samples.Length);
            }
        }
    }
}