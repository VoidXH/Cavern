using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Basic equalizer generation after any measurement was converted to a graph with
    /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/>.
    /// </summary>
    public static class EQCorrections {
        /// <summary>
        /// Generate a precise equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.The maximum gain on any band
        /// will not pass the recommended maximum of 6 dB.
        /// </summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing
        /// (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer AutoCorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain) =>
            AutoCorrectGraph(graph, startFreq, endFreq, targetCurve, targetGain, 6);

        /// <summary>
        /// Generate a precise equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.
        /// </summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing
        /// (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer AutoCorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain, float maxGain) {
            List<Band> bands = new List<Band>();
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / graph.Length;
            List<int> windowEdges = new List<int> { 0 };
            for (int sample = 1, end = graph.Length - 1; sample < end; ++sample) {
                if ((graph[sample - 1] < graph[sample] && graph[sample + 1] > graph[sample]) ||
                    (graph[sample - 1] > graph[sample] && graph[sample + 1] < graph[sample])) {
                    windowEdges.Add(sample);
                }
            }
            float[] refGain = targetCurve.GenerateLogCurve(startFreq, endFreq, graph.Length, targetGain);
            for (int sample = 0, end = windowEdges.Count - 1; sample < end; ++sample) {
                int windowPos = windowEdges[sample];
                if (graph[windowPos] > refGain[windowPos] - maxGain) {
                    bands.Add(new Band((float)Math.Pow(10, startPow + powRange * windowPos), refGain[windowPos] - graph[windowPos]));
                }
            }
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Generate an equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>. The maximum gain on any band
        /// will not pass the recommended maximum of 6 dB. The default resolution of 1/3 octaves will be used.
        /// </summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing
        /// (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain) =>
            CorrectGraph(graph, startFreq, endFreq, targetCurve, targetGain, 1 / 3f, 6);

        /// <summary>
        /// Generate an equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>. The maximum gain on any band
        /// will not pass the recommended maximum of 6 dB.
        /// </summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing
        /// (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        /// <param name="resolution">Band diversity in octaves</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain, double resolution) =>
            CorrectGraph(graph, startFreq, endFreq, targetCurve, targetGain, resolution, 6);

        /// <summary>
        /// Generate an equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.
        /// </summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing
        /// (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        /// <param name="resolution">Band diversity in octaves</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain, double resolution, float maxGain) {
            List<Band> bands = new List<Band>();
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(endFreq) - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2);
            int windowSize = graph.Length / (int)(octaveRange / resolution + 1), windowEdge = windowSize / 2;
            if (windowEdge == 0) {
                windowEdge = 1;
            }
            float[] refGain = targetCurve.GenerateLogCurve(startFreq, endFreq, graph.Length);
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize) {
                float centerFreq = (float)Math.Pow(10, startPow + powRange * pos), average = 0;
                int start = Math.Max(pos - windowEdge, 0), end = Math.Min(pos + windowEdge, graph.Length);
                for (int sample = start; sample < end; ++sample) {
                    average += graph[sample];
                }
                float addition = refGain[pos] + targetGain - average / (end - start);
                if (addition <= maxGain) {
                    bands.Add(new Band(centerFreq, addition));
                }
            }
            bands.Reverse();
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Create an EQ that completely linearizes the <paramref name="spectrum"/>.
        /// </summary>
        public static Equalizer FlattenSpectrum(Complex[] spectrum, int sampleRate) {
            double step = (double)sampleRate / spectrum.Length;
            List<Band> bands = new List<Band>(spectrum.Length >> 1);
            for (int i = 0; i < spectrum.Length >> 1; ++i) {
                bands.Add(new Band(i * step, -20 * Math.Log10(spectrum[i].Magnitude)));
            }
            return new Equalizer(bands, true);
        }
    }
}
