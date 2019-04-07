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
        /// <param name="Frequency">Crossover frequency</param>
        public Crossover(float Frequency) {
            LPF1 = new Lowpass(Frequency);
            LPF2 = new Lowpass(Frequency);
            HPF1 = new Highpass(Frequency);
            HPF2 = new Highpass(Frequency);
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public override void Process(float[] Samples, int Channel, int Channels) {
            int SampleCount = Samples.Length;
            if (SampleCount != LowOutput.Length) {
                LowOutput = new float[SampleCount];
                HighOutput = new float[SampleCount];
            }
            Array.Copy(Samples, LowOutput, SampleCount);
            Array.Copy(Samples, HighOutput, SampleCount);
            LPF1.Process(LowOutput, Channel, Channels);
            LPF2.Process(LowOutput, Channel, Channels);
            HPF1.Process(HighOutput, Channel, Channels);
            HPF2.Process(HighOutput, Channel, Channels);
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Lows">Low frequency data</param>
        /// <param name="Highs">High frequency data</param>
        public void Process(float[] Samples, out float[] Lows, out float[] Highs) {
            Process(Samples);
            Lows = LowOutput;
            Highs = HighOutput;
        }

        /// <summary>Apply crossover on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        /// <param name="Lows">Low frequency data</param>
        /// <param name="Highs">High frequency data</param>
        public void Process(float[] Samples, int Channel, int Channels, out float[] Lows, out float[] Highs) {
            Process(Samples, Channel, Channels);
            Lows = LowOutput;
            Highs = HighOutput;
        }
    }
}