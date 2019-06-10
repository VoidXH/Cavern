using System;

namespace Cavern.Filters {
    /// <summary>Simple second-order crossover.</summary>
    public class Crossover : Filter {
        /// <summary>Crossover frequency.</summary>
        public float Frequency {
            get => LPF1.CenterFreq;
            set => LPF1.CenterFreq = LPF2.CenterFreq = HPF1.CenterFreq = HPF2.CenterFreq = value;
        }

        /// <summary>Low frequency data.</summary>
        public float[] LowOutput { get; private set; } = new float[0];
        /// <summary>High frequency data.</summary>
        public float[] HighOutput { get; private set; } = new float[0];

        // Filters used. Second order is required for the prevention of a notch after mixing the outputs together.
        readonly Lowpass LPF1, LPF2;
        readonly Highpass HPF1, HPF2;

        /// <summary>Simple second-order crossover.</summary>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="frequency">Crossover frequency</param>
        public Crossover(int sampleRate, float frequency) {
            LPF1 = new Lowpass(sampleRate, frequency);
            LPF2 = new Lowpass(sampleRate, frequency);
            HPF1 = new Highpass(sampleRate, frequency);
            HPF2 = new Highpass(sampleRate, frequency);
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
            LPF1.Process(LowOutput, channel, channels);
            LPF2.Process(LowOutput, channel, channels);
            HPF1.Process(HighOutput, channel, channels);
            HPF2.Process(HighOutput, channel, channels);
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