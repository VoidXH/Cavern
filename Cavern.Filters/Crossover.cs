﻿using System;

namespace Cavern.Filters {
    /// <summary>Simple second-order crossover.</summary>
    public class Crossover : Filter {
        /// <summary>Crossover frequency.</summary>
        public float Frequency {
            get => lowpasses[0].CenterFreq;
            set {
                for (int i = 0; i < lowpasses.Length; ++i)
                    lowpasses[i].CenterFreq = highpasses[i].CenterFreq = value;
            }
        }

        /// <summary>Number of filters per pass.</summary>
        /// <remarks>A value of 2 is recommended for notch prevention when mixing
        /// <see cref="LowOutput"/> and <see cref="HighOutput"/> back together.</remarks>
        public int Order {
            get => lowpasses.Length;
            set => RecreateFilters(lowpasses[0].CenterFreq, value);
        }

        /// <summary>Low frequency data.</summary>
        public float[] LowOutput { get; private set; } = new float[0];
        /// <summary>High frequency data.</summary>
        public float[] HighOutput { get; private set; } = new float[0];

        /// <summary>Cached filter sample rate.</summary>
        readonly int sampleRate;
        /// <summary>Lowpass filters for each pass.</summary>
        Lowpass[] lowpasses;
        /// <summary>Highpass filters for each pass.</summary>
        Highpass[] highpasses;

        /// <summary>Create filters for each pass.</summary>
        void RecreateFilters(float frequency, int order) {
            lowpasses = new Lowpass[order];
            highpasses = new Highpass[order];
            for (int i = 0; i < order; ++i) {
                lowpasses[i] = new Lowpass(sampleRate, frequency);
                highpasses[i] = new Highpass(sampleRate, frequency);
            }
        }

        /// <summary>Simple second-order crossover.</summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="frequency">Crossover frequency</param>
        /// <param name="order">Number of filters per pass, 2 is recommended for mixing notch prevention</param>
        public Crossover(int sampleRate, float frequency, int order = 2) {
            this.sampleRate = sampleRate;
            RecreateFilters(frequency, order);
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            int sampleCount = samples.Length;
            if (sampleCount != LowOutput.Length) {
                LowOutput = new float[sampleCount];
                HighOutput = new float[sampleCount];
            }
            Array.Copy(samples, LowOutput, sampleCount);
            Array.Copy(samples, HighOutput, sampleCount);
            for (int i = 0; i < lowpasses.Length; ++i) {
                lowpasses[i].Process(LowOutput, channel, channels);
                highpasses[i].Process(HighOutput, channel, channels);
            }
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="lows">Low frequency data</param>
        /// <param name="highs">High frequency data</param>
        public void Process(float[] samples, out float[] lows, out float[] highs) {
            Process(samples);
            lows = LowOutput;
            highs = HighOutput;
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        /// <param name="lows">Low frequency data</param>
        /// <param name="highs">High frequency data</param>
        public void Process(float[] samples, int channel, int channels, out float[] lows, out float[] highs) {
            Process(samples, channel, channels);
            lows = LowOutput;
            highs = HighOutput;
        }
    }
}