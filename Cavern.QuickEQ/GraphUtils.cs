using Cavern.Utilities;
using System;

namespace Cavern.QuickEQ {
    /// <summary>Functions for graph processing and iterations.</summary>
    public static class GraphUtils {
        /// <summary>Action performed at a given frequency-value pair.</summary>
        public delegate void FrequencyFunction<T>(double frequency, ref T value);

        /// <summary>Perform an action for each frequency value on a linearly scaled graph or spectrum band.</summary>
        /// <param name="source">Sample array or spectrum</param>
        /// <param name="startFreq">Frequency at the first element of the array</param>
        /// <param name="endFreq">Frequency at the last element of the array</param>
        /// <param name="action">Performed action</param>
        public static void ForEachLin<T>(T[] source, double startFreq, double endFreq, FrequencyFunction<T> action) {
            double step = (endFreq - startFreq) / (source.Length - 1);
            for (int entry = 0; entry < source.Length; ++entry)
                action(startFreq + step * entry, ref source[entry]);
        }

        /// <summary>Perform an action for each frequency value on a logarithmically scaled graph or spectrum band.</summary>
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

        /// <summary>Convert a response curve back from decibel scale.</summary>
        public static void ConvertFromDecibels(float[] curve) {
            for (int i = 0; i < curve.Length; ++i)
                curve[i] = (float)Math.Pow(10, curve[i] * .05f);
        }

        /// <summary>Convert a response curve to decibel scale.</summary>
        public static void ConvertToDecibels(float[] curve, float minimum = -100) {
            for (int i = 0; i < curve.Length; ++i) {
                curve[i] = 20 * (float)Math.Log10(curve[i]);
                if (curve[i] < minimum) // this is also true if curve[i] == 0
                    curve[i] = minimum;
            }
        }

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
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

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
        /// <param name="response">Source response</param>
        /// <param name="startFreq">Frequency at the first position of the output</param>
        /// <param name="endFreq">Frequency at the last position of the output</param>
        /// <param name="sampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="resultSize">Length of the resulting array</param>
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

        /// <summary>Scales a graph to another length, while keeping the local peaks.</summary>
        public static float[] Scale(float[] source, int newLength) {
            float[] result = new float[newLength];
            float scale = source.Length / (float)newLength--;
            for (int pos = 0; pos < newLength; ++pos)
                result[pos] = WaveformUtils.GetPeakSigned(source, (int)(pos * scale), (int)((pos + 1) * scale));
            return result;
        }

        /// <summary>Scales a partial graph to another length, while keeping the local peaks.</summary>
        public static float[] Scale(float[] source, int newLength, int sourceStart, int sourceEnd) {
            float[] result = new float[newLength];
            float scale = (sourceEnd - sourceStart) / (float)newLength--;
            for (int pos = 0; pos < newLength; ++pos)
                result[pos] = WaveformUtils.GetPeakSigned(source, sourceStart + (int)(pos * scale), sourceStart + (int)((pos + 1) * scale));
            return result;
        }

        /// <summary>Apply smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], double, double, int, int)"/>.</summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float octave = 1 / 3f) {
            if (octave == 0)
                return (float[])samples.Clone();
            double octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2);
            int length = samples.Length, windowSize = (int)(length * octave / octaveRange), lastBlock = length - windowSize;
            float[] smoothed = new float[length--];
            float average = 0;
            for (int sample = 0; sample < windowSize; ++sample)
                average += samples[sample];
            for (int sample = 0; sample < windowSize; ++sample) {
                smoothed[sample] = average / (sample + windowSize);
                average += samples[sample + windowSize];
            }
            for (int sample = windowSize; sample < lastBlock; ++sample) {
                average += samples[sample + windowSize] - samples[sample - windowSize];
                smoothed[sample] = average / (windowSize << 1);
            }
            for (int sample = lastBlock; sample <= length; ++sample) {
                average -= samples[sample - windowSize];
                smoothed[sample] = average / (length - sample + windowSize);
            }
            return smoothed;
        }

        /// <summary>Apply variable smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], double, double, int, int)"/>.
        /// </summary>
        public static float[] SmoothGraph(float[] samples, float startFreq, float endFreq, float startOctave, float endOctave) {
            float[] startGraph = SmoothGraph(samples, startFreq, endFreq, startOctave), endGraph = SmoothGraph(samples, startFreq, endFreq, endOctave),
                output = new float[samples.Length];
            float positioner = 1f / samples.Length;
            for (int i = 0; i < samples.Length; ++i)
                output[i] = QMath.Lerp(startGraph[i], endGraph[i], i * positioner);
            return output;
        }
    }
}