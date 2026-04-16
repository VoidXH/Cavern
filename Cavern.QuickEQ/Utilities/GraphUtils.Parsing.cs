using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    partial class GraphUtils {
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
            double step = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (graph.Length - 1));
            startFreq *= response.Length / (double)sampleRate;
            for (int i = 0; i < graph.Length; i++) {
                graph[i] = response[(int)startFreq].Magnitude;
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
            double step = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / graph.Length);
            startFreq *= response.Length * 2 / (double)sampleRate;
            for (int i = 0; i < graph.Length; i++) {
                graph[i] = response[(int)startFreq];
                startFreq *= step;
            }
            return graph;
        }
    }
}
