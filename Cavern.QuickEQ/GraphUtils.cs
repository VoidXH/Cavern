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
            double startLog = Math.Log10(startFreq), endLog = Math.Log10(endFreq), mul = Math.Pow(10, (endLog - startLog) / (source.Length - 1));
            for (int i = 0; i < source.Length; ++i) {
                action(startFreq, ref source[i]);
                startFreq *= mul;
            }
        }

        /// <summary>Convert a response curve to decibel scale.</summary>
        public static void ConvertToDecibels(float[] curve, float minimum = -100) {
            for (int i = 0; i < curve.Length; ++i) {
                curve[i] = 20 * (float)Math.Log10(curve[i]);
                if (curve[i] < minimum)
                    curve[i] = minimum;
            }
        }

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
        /// <param name="samples">Source response</param>
        /// <param name="startFreq">Frequency at the first position of the output</param>
        /// <param name="endFreq">Frequency at the last position of the output</param>
        /// <param name="sampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="resultSize">Length of the resulting array</param>
        public static float[] ConvertToGraph(float[] samples, double startFreq, double endFreq, int sampleRate, int resultSize) {
            double freqRange = endFreq - startFreq, nyquist = sampleRate / 2.0;
            float[] graph = new float[resultSize];
            ForEachLog(graph, startFreq, endFreq, (double freq, ref float value) => value = samples[(int)(samples.Length * freq / nyquist)]);
            return graph;
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
                int newSample = sample + windowSize;
                average += samples[newSample];
                smoothed[sample] = average / newSample;
            }
            for (int sample = windowSize; sample < lastBlock; ++sample) {
                int oldSample = sample - windowSize, newSample = sample + windowSize;
                average += samples[newSample] - samples[oldSample];
                smoothed[sample] = average / (newSample - oldSample);
            }
            for (int sample = lastBlock; sample <= length; ++sample) {
                int oldSample = sample - windowSize;
                average -= samples[oldSample];
                smoothed[sample] = average / (length - oldSample);
            }
            return smoothed;
        }

        /// <summary>Apply variable smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], double, double, int, int)"/>.</summary>
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