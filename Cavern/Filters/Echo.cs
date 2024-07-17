using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple echo/reverberation filter with delay.
    /// </summary>
    public class Echo : Filter, ISampleRateDependentFilter, ILocalizableToString {
        /// <inheritdoc/>
        [IgnoreDataMember]
        public int SampleRate {
            get => sampleRate;
            set {
                double oldDelayTime = DelayTime;
                sampleRate = value;
                DelayTime = oldDelayTime;
            }
        }
        int sampleRate;

        /// <summary>
        /// Effect strength.
        /// </summary>
        public double Strength { get; set; }

        /// <summary>
        /// Delay between echoes in samples.
        /// </summary>
        [DisplayName("Echo interval (samples)")]
        public int DelaySamples {
            get => delay;
            set => Reset(Strength, value);
        }

        /// <summary>
        /// Delay between echoes in seconds.
        /// </summary>
        [DisplayName("Echo interval (seconds)")]
        public double DelayTime {
            get => delay / (double)SampleRate;
            set => Reset(Strength, value);
        }

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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override object Clone() => new Echo(sampleRate, Strength, delay);

        /// <inheritdoc/>
        public override string ToString() => $"Echo: {Strength}x, {DelayTime} s";

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" => $"Visszhang: {Strength}x, {DelayTime} mp",
            _ => ToString()
        };
    }
}