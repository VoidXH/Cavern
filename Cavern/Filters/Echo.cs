using System.Runtime.CompilerServices;

namespace Cavern.Filters {
    /// <summary>
    /// Simple echo/reverberation filter with delay.
    /// </summary>
    public class Echo : Filter {
        /// <summary>
        /// Effect strength.
        /// </summary>
        public double Strength;

        /// <summary>
        /// Delay between echoes in samples.
        /// </summary>
        public int DelaySamples {
            get => delay;
            set => Reset(Strength, value);
        }

        /// <summary>
        /// Delay between echoes in seconds.
        /// </summary>
        public double DelayTime {
            get => delay / (double)sampleRate;
            set => Reset(Strength, value);
        }

        /// <summary>
        /// Cached audio sample rate.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Samples to mix back to the next block.
        /// </summary>
        float[] cache;

        /// <summary>
        /// Cache is a loop, this is the current position.
        /// </summary>
        int cachePos;

        /// <summary>
        /// Delay between echoes in samples.
        /// </summary>
        int delay;

        /// <summary>
        /// Create an echo filter with the default effect strength (0.25) and delay (0.1 seconds).
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Echo(int sampleRate) : this(sampleRate, .25f, .1) { }

        /// <summary>
        /// Create an echo filter with a custom effect strength and default delay (0.1 seconds).
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="strength">Effect strength</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Echo(int sampleRate, double strength) : this(sampleRate, strength, .1) { }

        /// <summary>
        /// Create an echo filter with custom effect strength and delay.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="strength">Effect strength</param>
        /// <param name="delay">Delay between echoes in samples</param>
        public Echo(int sampleRate, double strength, int delay) {
            this.sampleRate = sampleRate;
            Reset(strength, delay);
        }

        /// <summary>
        /// Create an echo filter with custom effect strength and delay.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="strength">Effect strength</param>
        /// <param name="delay">Delay between echoes in seconds</param>
        public Echo(int sampleRate, double strength, double delay) => Reset(strength, (int)(delay * (this.sampleRate = sampleRate)));

        /// <summary>
        /// Reset filter settings.
        /// </summary>
        /// <param name="strength">Effect strength</param>
        /// <param name="delay">Delay between echoes in samples</param>
        public void Reset(double strength, int delay) {
            Strength = strength;
            this.delay = delay;
            cache = new float[delay];
            cachePos = 0;
        }

        /// <summary>
        /// Reset filter settings.
        /// </summary>
        /// <param name="strength">Effect strength</param>
        /// <param name="delay">Delay between echoes in seconds</param>
        public void Reset(double strength, double delay) => Reset(strength, (int)(delay * sampleRate));

        /// <summary>
        /// Apply echo on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            if (delay <= 0) {
                return;
            }
            float gain = (float)(1 / (1 + Strength)), strength = (float)Strength;
            for (int sample = channel; sample < samples.Length; sample += channels) {
                samples[sample] = (samples[sample] + cache[cachePos]) * gain;
                cache[cachePos] = samples[sample] * strength;
                cachePos = (cachePos + 1) % delay;
            }
        }
    }
}