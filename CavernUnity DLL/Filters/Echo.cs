namespace Cavern.Filters {
    /// <summary>Simple echo/reverberation filter with delay.</summary>
    public class Echo : Filter {
        /// <summary>Effect strength.</summary>
        public float Strength;
        /// <summary>Delay between echoes in samples.</summary>
        public int DelaySamples {
            get => Delay;
            set => Reset(Strength, value);
        }
        /// <summary>Delay between echoes in seconds.</summary>
        public float DelayTime {
            get => Delay / (float)AudioListener3D.Current.SampleRate;
            set => Reset(Strength, value);
        }

        /// <summary>Delay between echoes in samples.</summary>
        int Delay;
        /// <summary>Samples to mix back to the next block.</summary>
        float[] Cache;
        /// <summary>Cache is a loop, this is the current position.</summary>
        int CachePos;

        /// <summary>Create an echo filter.</summary>
        /// <param name="Strength">Effect strength</param>
        /// <param name="Delay">Delay between echoes in samples</param>
        public Echo(float Strength = .25f, int Delay = 4096) => Reset(Strength, Delay);

        /// <summary>Create an echo filter.</summary>
        /// <param name="Strength">Effect strength</param>
        /// <param name="Delay">Delay between echoes in seconds</param>
        public Echo(float Strength = .25f, float Delay = .1f) => Reset(Strength, (int)(Delay * AudioListener3D.Current.SampleRate));

        /// <summary>Reset filter settings.</summary>
        /// <param name="Strength">Effect strength</param>
        /// <param name="Delay">Delay between echoes in samples</param>
        public void Reset(float Strength, int Delay) {
            this.Strength = Strength;
            this.Delay = Delay;
            Cache = new float[Delay];
            CachePos = 0;
        }

        /// <summary>Reset filter settings.</summary>
        /// <param name="Strength">Effect strength</param>
        /// <param name="Delay">Delay between echoes in seconds</param>
        public void Reset(float Strength, float Delay) => Reset(Strength, (int)(Delay * AudioListener3D.Current.SampleRate));

        /// <summary>Apply echo on a set of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] Samples) => Process(Samples, 0, 1);

        /// <summary>Apply echo on a set of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public override void Process(float[] Samples, int Channel, int Channels) {
            if (Delay <= 0)
                return;
            float Gain = 1 / (1 + Strength);
            for (int Sample = Channel, Length = Samples.Length; Sample < Length; Sample += Channels) {
                Samples[Sample] = (Samples[Sample] + Cache[CachePos]) * Gain;
                Cache[CachePos] = Samples[Sample] * Strength;
                CachePos = (CachePos + 1) % Delay;
            }
        }
    }
}