using System;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.SignalGeneration {
    /// <summary>
    /// Generates various signals.
    /// </summary>
    public class WaveformGenerator {
        /// <summary>
        /// Generates a sine wave signal.
        /// </summary>
        /// <param name="frequency">The frequency of the sine wave in Hertz</param>
        /// <param name="length">The length of the generated signal in samples</param>
        /// <remarks>No windowing is used, and a click will be heard if the last period doesn't end at
        /// the <paramref name="length"/>.</remarks>
        public static float[] Sine(float frequency, int length) {
            float[] result = new float[length];
            float mul = 2 * MathF.PI * frequency / length;
            for (int i = 0; i < length; i++) {
                result[i] = MathF.Sin(mul * i);
            }
            return result;
        }

        /// <summary>
        /// Generates white noise.
        /// </summary>
        /// <param name="length">The length of the generated noise in samples</param>
        public static float[] WhiteNoise(int length) {
            float[] result = new float[length];
            Random generator = new Random();
            for (int i = 0; i < length; i++) {
                result[i] = (float)(generator.NextDouble() * 2 - 1);
            }
            return result;
        }

        /// <summary>
        /// Generates precise pink noise through EQing white noise.
        /// </summary>
        /// <param name="length">The length of the generated noise in samples</param>
        /// <param name="sampleRate">Sample rate of the </param>
        public static float[] PinkNoise(int length, int sampleRate) {
            int workingLength = QMath.Base2Ceil(length);
            float[] result = WhiteNoise(workingLength);
            Equalizer eq = new Equalizer();
            const double startFreq = 10;
            double nyquist = sampleRate / 2;
            eq.AddBand(new Band(startFreq, 0));
            // Pink noise bands lose 3 dB/octave, which is 10 dB/decade
            eq.AddBand(new Band(nyquist, -10 * Math.Log(nyquist) / Math.Log(startFreq)));
            eq.DownsampleLogarithmically(4096, startFreq, nyquist); // TODO: remove when EQ visualizations work in log space
            result = FastConvolver.ConvolveSafe(result, eq.GetConvolution(sampleRate, workingLength));
            Array.Resize(ref result, length);
            WaveformUtils.Gain(result, 10); // Stable normalization
            return result;
        }
    }
}