using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Functions for graph processing and iterations.
    /// </summary>
    public static class GraphUtils {
        /// <summary>
        /// Action performed at a given frequency-value pair.
        /// </summary>
        public delegate void FrequencyFunction<T>(double frequency, ref T value);

        /// <summary>
        /// Perform an action for each frequency value on a linearly scaled graph or spectrum band.
        /// </summary>
        /// <param name="source">Sample array or spectrum</param>
        /// <param name="startFreq">Frequency at the first element of the array</param>
        /// <param name="endFreq">Frequency at the last element of the array</param>
        /// <param name="action">Performed action</param>
        public static void ForEachLin<T>(T[] source, double startFreq, double endFreq, FrequencyFunction<T> action) {
            double step = (endFreq - startFreq) / (source.Length - 1);
            for (int entry = 0; entry < source.Length; ++entry) {
                action(startFreq + step * entry, ref source[entry]);
            }
        }

        /// <summary>
        /// Perform an action for each frequency value on a logarithmically scaled graph or spectrum band.
        /// </summary>
        /// <param name="source">Sample array or spectrum</param>
        /// <param name="startFreq">Frequency at the first element of the array</param>
        /// <param name="endFreq">Frequency at the last element of the array</param>
        /// <param name="action">Performed action</param>
        public static void ForEachLog<T>(T[] source, double startFreq, double endFreq, FrequencyFunction<T> action) {
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (source.Length - 1));
            for (int i = 0; i < source.Length; ++i) {
                action(startFreq, ref source[i]);
                startFreq *= mul;
            }
        }

        /// <summary>
        /// Apply a slope (in decibels from start to finish) to an existing curve.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddSlope(this float[] curve, float slope) {
            float mul = 1f / curve.Length;
            for (int i = 0; i < curve.Length; ++i) {
                curve[i] += slope * mul;
            }
        }

        /// <summary>
        /// Convert a response curve back from decibel scale.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertFromDecibels(float[] curve) {
            for (int i = 0; i < curve.Length; ++i) {
                curve[i] = (float)Math.Pow(10, curve[i] * .05f);
            }
        }

        /// <summary>
        /// Convert a response curve to decibel scale.
        /// </summary>
        /// <remarks>The minimum value will be -100 to prevent NaNs.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToDecibels(float[] curve) => ConvertToDecibels(curve, -100);

        /// <summary>
        /// Convert a response curve to decibel scale.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToDecibels(float[] curve, float minimum) {
            for (int i = 0; i < curve.Length; ++i) {
                curve[i] = 20 * (float)Math.Log10(curve[i]);
                if (curve[i] < minimum) { // this is also true if curve[i] == 0
                    curve[i] = minimum;
                }
            }
        }

        /// <summary>
        /// Convert a response to logarithmically scaled frequency range.
        /// </summary>
        /// <param name="response">Source response</param>
        /// <param name="startFreq">Frequency at the first position of the output</param>
        /// <param name="endFreq">Frequency at the last position of the output</param>
        /// <param name="sampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="resultSize">Length of the resulting array</param>
        public static float[] ConvertToGraph(Complex[] response, double startFreq, double endFreq, int sampleRate, int resultSize) {
            float[] graph = new float[resultSize];
            double step = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (graph.Length - 1)),
                positioner = response.Length / (double)sampleRate;
            for (int i = 0; i < graph.Length; ++i) {
                graph[i] = response[(int)(startFreq * positioner)].Magnitude;
                startFreq *= step;
            }
            return graph;
        }

        /// <summary>
        /// Convert a response to logarithmically scaled cut frequency range.
        /// </summary>
        /// <param name="response">Source response</param>
        /// <param name="startFreq">Frequency at the first position of the output</param>
        /// <param name="endFreq">Frequency at the last position of the output</param>
        /// <param name="sampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="resultSize">Length of the resulting array</param>
        /// <remarks>Requires a response that is half the FFT size (only extends to <paramref name="sampleRate"/> Hz),
        /// for example the result of <see cref="Measurements.GetSpectrum(Complex[])"/>.</remarks>
        public static float[] ConvertToGraph(float[] response, double startFreq, double endFreq, int sampleRate, int resultSize) {
            float[] graph = new float[resultSize];
            double step = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (graph.Length - 1)),
                positioner = response.Length * 2 / (double)sampleRate;
            for (int i = 0; i < graph.Length; ++i) {
                graph[i] = response[(int)(startFreq * positioner)];
                startFreq *= step;
            }
            return graph;
        }

        /// <summary>
        /// Get the correlation between two curves.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Correlation(float[] x, float[] y) => Correlation(x, y, 0, x.Length);

        /// <summary>
        /// Get the correlation between a <paramref name="start"/> and <paramref name="end"/> index.
        /// </summary>
        public static float Correlation(float[] x, float[] y, int start, int end) {
            float xMean = x.Average(), yMean = y.Average(),
                sumXY = 0, sumX2 = 0, sumY2 = 0;
            for (int i = start; i < end; i++) {
                float xDiff = x[i] - xMean, yDiff = y[i] - yMean;
                sumXY += xDiff * yDiff;
                sumX2 += xDiff * xDiff;
                sumY2 += yDiff * yDiff;
            }
            return sumXY / MathF.Sqrt(sumX2 * sumY2);
        }

        /// <summary>
        /// Get both the minimum and maximum values of the graph.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float min, float max) GetLimits(float[] values) {
            float min = values[0];
            float max = values[0];
            for (int i = 1; i < values.Length; ++i) {
                if (max < values[i]) {
                    max = values[i];
                }
                if (min > values[i]) {
                    min = values[i];
                }
            }
            return (min, max);
        }

        /// <summary>
        /// Get the peak value of a graph.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(this float[] graph) {
            float max = graph[0];
            for (int i = 1; i < graph.Length; i++) {
                if (max < graph[i]) {
                    max = graph[i];
                }
            }
            return max;
        }

        /// <summary>
        /// Moves a graph's average value to 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(float[] graph) {
            float avg = graph.Average();
            for (int i = 0; i < graph.Length; ++i) {
                graph[i] -= avg;
            }
        }

        /// <summary>
        /// Scales a graph to another length, while keeping the local peaks.
        /// </summary>
        public static float[] Scale(float[] source, int newLength) {
            float[] result = new float[newLength];
            float scale = source.Length / (float)newLength--;
            for (int pos = 0; pos < newLength; ++pos) {
                result[pos] = WaveformUtils.GetPeakSigned(source, (int)(pos * scale), (int)((pos + 1) * scale));
            }
            return result;
        }

        /// <summary>
        /// Scales a partial graph to another length, while keeping the local peaks.
        /// </summary>
        public static float[] Scale(float[] source, int newLength, int sourceStart, int sourceEnd) {
            float[] result = new float[newLength];
            float scale = (sourceEnd - sourceStart) / (float)newLength--;
            for (int pos = 0; pos < newLength; ++pos) {
                result[pos] =
                    WaveformUtils.GetPeakSigned(source, sourceStart + (int)(pos * scale), sourceStart + (int)((pos + 1) * scale));
            }
            return result;
        }

        /// <summary>
        /// Smooth any kind of graph with a uniform window size.
        /// </summary>
        public static float[] SmoothUniform(float[] source, int windowSize) {
            int length = source.Length,
                lastBlock = length - windowSize;
            float[] smoothed = new float[length--];
            float average = 0;
            for (int sample = 0; sample < windowSize; ++sample) {
                average += source[sample];
            }
            for (int sample = 0; sample < windowSize; ++sample) {
                smoothed[sample] = average / (sample + windowSize);
                average += source[sample + windowSize];
            }
            for (int sample = windowSize; sample < lastBlock; ++sample) {
                average += source[sample + windowSize] - source[sample - windowSize];
                smoothed[sample] = average / (windowSize * 2);
            }
            for (int sample = lastBlock; sample <= length; ++sample) {
                average -= source[sample - windowSize];
                smoothed[sample] = average / (length - sample + windowSize);
            }
            return smoothed;
        }

        /// <summary>
        /// Apply 1/3 octave smoothing on a graph drawn with <see cref="ConvertToGraph(float[], double, double, int, int)"/>.
        /// </summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq) =>
            SmoothGraph(samples, startFreq, endFreq, 1 / 3f);

        /// <summary>
        /// Apply smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], double, double, int, int)"/>.
        /// </summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float octave) {
            if (octave == 0) {
                return samples.FastClone();
            }
            double octaveRange = Math.Log(endFreq, 2);
            if (startFreq != 0) {
                octaveRange -= Math.Log(startFreq, 2);
            }
            int windowSize = (int)(samples.Length * octave / octaveRange);
            return SmoothUniform(samples, windowSize);
        }

        /// <summary>
        /// Apply variable smoothing (in octaves) on a graph drawn with
        /// <see cref="ConvertToGraph(float[], double, double, int, int)"/>.
        /// </summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float startOctave, float endOctave) {
            float[] startGraph = SmoothGraph(samples, startFreq, endFreq, startOctave),
                endGraph = SmoothGraph(samples, startFreq, endFreq, endOctave),
                output = new float[samples.Length];
            float positioner = 1f / samples.Length;
            for (int i = 0; i < samples.Length; ++i) {
                output[i] = QMath.Lerp(startGraph[i], endGraph[i], i * positioner);
            }
            return output;
        }
    }
}