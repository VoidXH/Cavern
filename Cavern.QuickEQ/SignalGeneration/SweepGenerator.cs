using System;

namespace Cavern.QuickEQ.SignalGeneration {
    /// <summary>
    /// Generates various sine sweep (chirp) signals.
    /// </summary>
    public static class SweepGenerator {
        /// <summary>
        /// Generate a linear frequency sweep with a flat frequency response.
        /// </summary>
        public static float[] Linear(double startFreq, double endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = (endFreq - startFreq) / (2 * samples / (double)sampleRate), mul = 1.0 / sampleRate;
            for (int sample = 0; sample < samples; ++sample) {
                double position = sample * mul;
                output[sample] = (float)Math.Sin(2 * Math.PI * (startFreq * position + chirpyness * position * position));
            }
            return output;
        }

        /// <summary>
        /// Generate the frequencies at each sample's position in a linear frequency sweep.
        /// </summary>
        public static float[] LinearFreqs(double startFreq, double endFreq, int samples) {
            float[] result = new float[samples];
            double step = (endFreq - startFreq) / (samples - 1);
            for (int entry = 0; entry < samples; ++entry) {
                result[entry] = (float)(startFreq + step * entry);
            }
            return result;
        }

        /// <summary>
        /// Generate an exponential frequency sweep.
        /// </summary>
        public static float[] Exponential(double startFreq, double endFreq, int samples, int sampleRate) {
            float[] output = new float[samples];
            double chirpyness = Math.Pow(endFreq / startFreq, sampleRate / (double)samples), mul = 1.0 / sampleRate,
                logChirpyness = Math.Log(chirpyness), sinConst = 2 * Math.PI * startFreq;
            for (int sample = 0; sample < samples; ++sample) {
                output[sample] = (float)Math.Sin(sinConst * (Math.Pow(chirpyness, sample * mul) - 1) / logChirpyness);
            }
            return output;
        }

        /// <summary>
        /// Generate the frequencies at each sample's position in an exponential frequency sweep.
        /// </summary>
        public static float[] ExponentialFreqs(double startFreq, double endFreq, int samples) {
            float[] result = new float[samples];
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (samples - 1));
            for (int i = 0; i < samples; ++i) {
                result[i] = (float)startFreq;
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>
        /// Add silence to the beginning and the end of a sweep for a larger response window.
        /// </summary>
        public static float[] Frame(float[] sweep) {
            float[] result = new float[sweep.Length * 2];
            int initialSilence = sweep.Length / 4;
            for (int sample = initialSilence, end = sweep.Length + initialSilence; sample < end; ++sample) {
                result[sample] = sweep[sample - initialSilence];
            }
            return result;
        }
    }
}