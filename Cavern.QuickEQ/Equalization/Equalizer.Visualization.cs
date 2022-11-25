using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Shows the resulting frequency response if this EQ is applied.
        /// </summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, double startFreq, double endFreq) {
            float[] filter = Visualize(startFreq, endFreq, response.Length);
            for (int i = 0; i < response.Length; ++i) {
                filter[i] += response[i];
            }
            return filter;
        }

        /// <summary>
        /// Apply this EQ on a frequency response.
        /// </summary>
        /// <param name="response">Frequency response to apply the EQ on</param>
        /// <param name="sampleRate">Sample rate where <paramref name="response"/> was generated</param>
        public void Apply(Complex[] response, int sampleRate) {
            int halfLength = response.Length / 2 + 1, nyquist = sampleRate / 2;
            float[] filter = VisualizeLinear(0, nyquist, halfLength);
            response[0] *= (float)Math.Pow(10, filter[0] * .05f);
            for (int i = 1; i < halfLength; ++i) {
                response[i] *= (float)Math.Pow(10, filter[i] * .05f);
                response[^i] = new Complex(response[i].Real, -response[i].Imaginary);
            }
        }

        /// <summary>
        /// Shows the EQ curve in a logarithmically scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0, nextBand = 0, prevBand = 0; i < length; ++i) {
                while (nextBand != bandCount && bands[nextBand].Frequency < startFreq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[i] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, startFreq));
                } else {
                    result[i] = (float)bands[prevBand].Gain;
                }
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>
        /// Shows the EQ curve in a linearly scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] VisualizeLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0, nextBand = 0, prevBand = 0; entry < length; ++entry) {
                double freq = startFreq + step * entry;
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[entry] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                } else {
                    result[entry] = (float)bands[prevBand].Gain;
                }
            }
            return result;
        }
    }
}