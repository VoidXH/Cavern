using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

using Cavern.Filters.Interfaces;

namespace Cavern.Filters {
    /// <summary>
    /// Delays the audio.
    /// </summary>
    public class Delay : Filter, IEqualizerAPOFilter {
        /// <summary>
        /// Delay in samples.
        /// </summary>
        [DisplayName("Delay (samples)")]
        public int DelaySamples {
            get => cache[0].Length;
            set {
                if (cache[0].Length != value) {
                    RecreateCaches(value);
                    delayMs = double.NaN;
                }
            }
        }

        /// <summary>
        /// Delay in milliseconds.
        /// </summary>
        [DisplayName("Delay (ms)")]
        public double DelayMs {
            get {
                if (!double.IsNaN(delayMs)) {
                    return delayMs;
                }
                if (sampleRate == 0) {
                    throw new SampleRateNotSetException();
                }
                return DelaySamples / (double)sampleRate * 1000;
            }

            set {
                if (sampleRate == 0) {
                    throw new SampleRateNotSetException();
                }
                DelaySamples = (int)Math.Round(value * sampleRate * .001);
                delayMs = value;
            }
        }

        /// <summary>
        /// When the filter was created with a precise delay that is not a round value in samples, display this instead.
        /// </summary>
        double delayMs;

        /// <summary>
        /// Cached samples for the next block. Alternates between two arrays to prevent memory allocation.
        /// </summary>
        readonly float[][] cache = new float[2][];

        /// <summary>
        /// If the filter was set up with a time delay, this is the sample rate that was used for it.
        /// </summary>
        readonly int sampleRate;

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
        public Delay(int samples) {
            delayMs = double.NaN;
            RecreateCaches(samples);
        }

        /// <summary>
        /// Create a delay for a given length in seconds.
        /// </summary>
        public Delay(double time, int sampleRate) {
            this.sampleRate = sampleRate;
            delayMs = time;
            RecreateCaches((int)(time * sampleRate * .001 + .5));
        }

        /// <summary>
        /// Parse a Delay line of Equalizer APO to a Cavern <see cref="Delay"/> filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delay FromEqualizerAPO(string line, int sampleRate) =>
            FromEqualizerAPO(line.Split(' ', StringSplitOptions.RemoveEmptyEntries), sampleRate);

        /// <summary>
        /// Parse a Delay line of Equalizer APO which was split at spaces to a Cavern <see cref="Delay"/> filter.
        /// </summary>
        public static Delay FromEqualizerAPO(string[] splitLine, int sampleRate) {
            double delay = double.Parse(splitLine[1].Replace(',', '.'), CultureInfo.InvariantCulture);
            return splitLine[2].ToLower(CultureInfo.InvariantCulture) switch {
                "ms" => new Delay(delay, sampleRate),
                "samples" => new Delay((int)delay),
                _ => throw new ArgumentOutOfRangeException(splitLine[0]),
            };
        }

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

        /// <inheritdoc/>
        public override string ToString() {
            if (sampleRate == 0) {
                return $"Delay: {DelaySamples} samples";
            } else {
                string delay = DelayMs.ToString(CultureInfo.InvariantCulture);
                return $"Delay: {delay} ms";
            }
        }

        /// <inheritdoc/>
        public void ExportToEqualizerAPO(List<string> wipConfig) => wipConfig.Add(ToString());
    }
}