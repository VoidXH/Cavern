namespace Cavern.Filters {
    /// <summary>Delays the audio.</summary>
    public class Delay : Filter {
        /// <summary>Delay in samples.</summary>
        public int DelaySamples {
            get => cache[0].Length;
            set {
                if (cache[0].Length != value)
                    RecreateCaches(value);
            }
        }

        /// <summary>Cached samples for the next block. Alternates between two arrays to prevent memory allocation.</summary>
        readonly float[][] cache = new float[2][];
        /// <summary>The used cache (0 or 1).</summary>
        int usedCache;

        void RecreateCaches(int size) {
            cache[0] = new float[size];
            cache[1] = new float[size];
        }

        /// <summary>Create a delay for a given length in samples.</summary>
        public Delay(int samples) => RecreateCaches(samples);

        /// <summary>Create a delay for a given length in seconds.</summary>
        public Delay(float time, int sampleRate) => RecreateCaches((int)(time * sampleRate + .5f));

        /// <summary>Apply delay on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] samples) {
            int delaySamples = cache[0].Length;
            float[] cacheToFill = cache[1 - usedCache], cacheToDrain = cache[usedCache];
            if (delaySamples <= samples.Length) { // Sample array can hold the cache
                // Fill cache
                for (int sample = 0, offset = samples.Length - delaySamples; sample < delaySamples; ++sample)
                    cacheToFill[sample] = samples[sample + offset];
                // Move self
                for (int sample = samples.Length - 1; sample >= delaySamples; --sample)
                    samples[sample] = samples[sample - delaySamples];
                // Drain cache
                for (int sample = 0; sample < delaySamples; ++sample)
                    samples[sample] = cacheToDrain[sample];
                usedCache = 1 - usedCache; // Switch caches
            } else { // Cache can hold the sample array
                // Fill cache
                for (int sample = 0; sample < samples.Length; ++sample)
                    cacheToFill[sample] = samples[sample];
                // Drain cache
                for (int sample = 0; sample < samples.Length; ++sample)
                    samples[sample] = cacheToDrain[sample];
                // Move cache
                for (int sample = 0, offset = delaySamples - samples.Length; sample < offset; ++sample)
                    cacheToDrain[sample] = cacheToDrain[sample + samples.Length];
                // Combine cache
                for (int sample = 0, offset = delaySamples - samples.Length; sample < samples.Length; ++sample)
                    cacheToDrain[sample + offset] = cacheToFill[sample];
            }
        }
    }
}