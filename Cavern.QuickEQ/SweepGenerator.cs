using System;

namespace Cavern.QuickEQ {
    /// <summary>Generates various sine sweep (chirp) signals.</summary>
    public static class SweepGenerator { // TODO: merge SweepChannelSource and seal
        /// <summary>Generate a linear frequency sweep with a flat frequency response.</summary>
        public static float[] Linear(double startFreq, double endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = (endFreq - startFreq) / (2 * samples / (double)sampleRate);
            for (int sample = 0; sample < samples; ++sample) {
                double position = sample / (double)sampleRate;
                output[sample] = (float)Math.Sin(2 * Math.PI * (startFreq * position + chirpyness * position * position));
            }
            return output;
        }

        /// <summary>Generate the frequencies at each sample's position in a linear frequency sweep.</summary>
        public static float[] LinearFreqs(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] freqs = new float[samples];
            float chirpyness = endFreq - startFreq / (samples / (float)sampleRate), mul = 1f / sampleRate;
            for (int sample = 0; sample < samples; ++sample)
                freqs[sample] = startFreq + chirpyness * sample * mul;
            return freqs;
        }

        /// <summary>Generate an exponential frequency sweep.</summary>
        public static float[] Exponential(double startFreq, double endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = Math.Pow(endFreq / startFreq, sampleRate / (double)samples),
                logChirpyness = Math.Log(chirpyness), sinConst = 2 * Math.PI * startFreq;
            for (int sample = 0; sample < samples; ++sample)
                output[sample] = (float)Math.Sin(sinConst * (Math.Pow(chirpyness, sample / (double)sampleRate) - 1) / logChirpyness);
            return output;
        }

        /// <summary>Generate the frequencies at each sample's position in an exponential frequency sweep.</summary>
        public static float[] ExponentialFreqs(float startFreq, float endFreq, int samples, int sampleRate) {
            float[] freqs = new float[samples];
            double chirpyness = Math.Pow(endFreq / startFreq, sampleRate / (double)samples), mul = 1.0 / sampleRate;
            for (int sample = 0; sample < samples; ++sample)
                freqs[sample] = startFreq + (float)Math.Pow(chirpyness, sample * mul);
            return freqs;
        }

        /// <summary>Add silence to the beginning and the end of a sweep for a larger response window.</summary>
        public static float[] Frame(float[] sweep) {
            float[] result = new float[sweep.Length * 2];
            for (int initialSilence = sweep.Length / 4, sample = initialSilence, end = sweep.Length + initialSilence; sample < end; ++sample)
                result[sample] = sweep[sample - initialSilence];
            return result;
        }
    }
}