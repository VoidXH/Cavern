﻿using System;
using System.ComponentModel;

namespace Cavern.Filters {
    /// <summary>
    /// Separates ground and height data for a channel of a regular surround mix.
    /// </summary>
    public class Cavernize : Filter {
        /// <summary>
        /// Height separation effect strength.
        /// </summary>
        [DisplayName("Effect (ratio)")]
        public float Effect { get; set; } = .75f;

        /// <summary>
        /// Ratio of the distance actually moved between calculated heights of ftames. Should be set with
        /// <see cref="CalculateSmoothingFactor(int, float)"/>.
        /// </summary>
        /// <remarks>The default value is calculated with 0.8 smoothness, with an update rate of 240 at
        /// 48 kHz sampling.</remarks>
        [DisplayName("Smoothing factor (ratio)")]
        public float SmoothFactor { get; set; } = .0229349384f;

        /// <summary>
        /// Keep all frequencies below this on the ground.
        /// </summary>
        [DisplayName("Ground crossover (Hz)")]
        public double GroundCrossover {
            get => crossover.Frequency;
            set => crossover.Frequency = value;
        }

        /// <summary>
        /// The moving part's normalized height from the ground. Clamped between -0.2 and 1, 1 means max height.
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// Audio that must be played at ground level. Results are from the last frame.
        /// </summary>
        public float[] GroundLevel => crossover.LowOutput;

        /// <summary>
        /// Audio that should be played at the level by <see cref="Height"/>. Results are from the last frame.
        /// </summary>
        public float[] HeightLevel => crossover.HighOutput;

        /// <summary>
        /// Crossover that mixes height sounds below its frequency back to the ground.
        /// </summary>
        readonly Crossover crossover;

        /// <summary>
        /// Last low frequency sample (used in the height calculation algorithm).
        /// </summary>
        float lastLow;

        /// <summary>
        /// Last unmodified sample (used in the height calculation algorithm).
        /// </summary>
        float lastNormal;

        /// <summary>
        /// Last high frequency sample (used in the height calculation algorithm).
        /// </summary>
        float lastHigh;

        /// <summary>
        /// Separates ground and height data for a channel of a regular surround mix. Constructs the filter with keeping sounds
        /// below 250 Hz on the ground.
        /// </summary>
        /// <param name="sampleRate">Content sample rate</param>
        public Cavernize(int sampleRate) : this(sampleRate, 250) { }

        /// <summary>
        /// Separates ground and height data for a channel of a regular surround mix.
        /// </summary>
        /// <param name="sampleRate">Content sample rate</param>
        /// <param name="crossoverFrequency">Keep sounds below this frequency on the ground layer</param>
        public Cavernize(int sampleRate, float crossoverFrequency) =>
            crossover = new Crossover(sampleRate, crossoverFrequency, 2);

        /// <summary>
        /// Generate the smoothing factor for a smoothness value.
        /// </summary>
        /// <param name="sampleRate">System sample rate</param>
        /// <param name="updateRate">Block size for processing</param>
        /// <param name="smoothness">Smoothness from 0 to 1</param>
        public static float CalculateSmoothingFactor(int sampleRate, int updateRate, float smoothness) =>
            1.001f - (updateRate + (sampleRate - updateRate) * MathF.Pow(smoothness, .1f)) / sampleRate;

        /// <summary>
        /// Generate the smoothing factor for a smoothness value.
        /// </summary>
        /// <param name="updateRate">Block size for processing</param>
        /// <param name="smoothness">Smoothness from 0 to 1</param>
        public void CalculateSmoothingFactor(int updateRate, float smoothness) =>
            SmoothFactor =
                1.001f - (updateRate + (crossover.SampleRate - updateRate) * MathF.Pow(smoothness, .1f)) / crossover.SampleRate;

        /// <summary>
        /// Cavernize an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        public override void Process(float[] samples) {
            crossover.Process(samples);
            float maxDepth = .0001f, maxHeight = .0001f, absHigh, absLow;
            for (int sample = 0; sample < samples.Length; ++sample) {
                // Height is generated by a simplified measurement of volume and pitch
                lastHigh = .9f * (lastHigh + samples[sample] - lastNormal);
                absHigh = Math.Abs(lastHigh);
                if (maxHeight < absHigh) {
                    maxHeight = absHigh;
                }
                lastLow = lastLow * .99f + lastHigh * .01f;
                absLow = Math.Abs(lastLow);
                if (maxDepth < absLow) {
                    maxDepth = absLow;
                }
                lastNormal = samples[sample];
            }

            maxHeight = (maxHeight - maxDepth * 1.2f) * 15 * Effect;
            if (maxHeight < -.2f) {
                maxHeight = -.2f;
            } else if (maxHeight > 1) {
                maxHeight = 1;
            }
            Height = (maxHeight - Height) * SmoothFactor + Height;
        }

        /// <summary>
        /// Create empty outputs for a given <paramref name="updateRate"/>> in case they are
        /// used before processing. This optimizes zero checks.
        /// </summary>
        public void PresetOutput(int updateRate) => crossover.PresetOutput(updateRate);
    }
}