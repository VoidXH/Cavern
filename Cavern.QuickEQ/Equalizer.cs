using System;
using System.Collections.Generic;

using Cavern.QuickEQ.EQCurves;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Equalizer data collector and exporter.</summary>
    public sealed class Equalizer {
        /// <summary>A single equalizer band.</summary>
        public struct Band {
            /// <summary>Position of the band.</summary>
            public readonly double frequency;
            /// <summary>Gain at <see cref="frequency"/> in dB.</summary>
            public readonly double gain;

            /// <summary>EQ band constructor.</summary>
            public Band(double frequency, double gain) {
                this.frequency = frequency;
                this.gain = gain;
            }
        }

        /// <summary>Bands that make up this equalizer.</summary>
        public IReadOnlyList<Band> Bands => bands;
        readonly List<Band> bands = new List<Band>();

        /// <summary>Subsonic filter rolloff in dB / octave.</summary>
        public double SubsonicRolloff {
            get => subsonicRolloff;
            set {
                bool wasFiltered = SubsonicFilter;
                if (wasFiltered)
                    SubsonicFilter = false;
                subsonicRolloff = value;
                if (wasFiltered)
                    SubsonicFilter = true;
            }
        }
        double subsonicRolloff = 24;

        /// <summary>Cut off low frequencies that are out of the channel's frequency range.</summary>
        public bool SubsonicFilter {
            get => subsonicFilter;
            set {
                if (subsonicFilter && !value) {
                    if (bands.Count > 0)
                        bands.RemoveAt(0);
                } else if (!subsonicFilter && value && bands.Count > 0)
                    AddBand(new Band(bands[0].frequency * .5f, bands[0].gain - subsonicRolloff));
                subsonicFilter = value;
            }
        }
        bool subsonicFilter = false;

        /// <summary>The highest gain in this EQ.</summary>
        public double PeakGain { get; private set; }

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band newBand) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            if (bands.Count == 0 || PeakGain < newBand.gain)
                PeakGain = newBand.gain;
            bands.Add(newBand);
            bands.Sort((a, b) => a.frequency.CompareTo(b.frequency));
            if (subFiltered)
                SubsonicFilter = true;
        }

        /// <summary>Remove a band from the EQ.</summary>
        public void RemoveBand(Band removable) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            bands.Remove(removable);
            if (bands.Count == 0)
                PeakGain = 0;
            else if (PeakGain == removable.gain) {
                PeakGain = bands[0].gain;
                for (int band = 1, count = bands.Count; band < count; ++band)
                    if (PeakGain > bands[band].gain)
                        PeakGain = bands[band].gain;
            }
            if (subFiltered)
                SubsonicFilter = true;
        }

        /// <summary>Reset this EQ.</summary>
        public void ClearBands() {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            PeakGain = 0;
            bands.Clear();
            if (subFiltered)
                SubsonicFilter = true;
        }

        /// <summary>Shows the EQ curve in a logarithmically scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count, nextBand = 0, prevBand = 0;
            if (bandCount == 0)
                return result;
            GraphUtils.ForEachLog(result, startFreq, endFreq, (double freq, ref float value) => {
                while (nextBand != bandCount && bands[nextBand].frequency < freq)
                    prevBand = ++nextBand - 1;
                if (nextBand != bandCount && nextBand != 0)
                    value = (float)QMath.Lerp(bands[prevBand].gain, bands[nextBand].gain,
                        QMath.LerpInverse(bands[prevBand].frequency, bands[nextBand].frequency, freq));
                else
                    value = (float)bands[prevBand].gain;
            });
            return result;
        }

        /// <summary>Shows the resulting frequency response if this EQ is applied.</summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, float startFreq, float endFreq) {
            float[] filter = Visualize(startFreq, endFreq, response.Length);
            for (int i = 0; i < response.Length; ++i)
                filter[i] += response[i];
            return filter;
        }

        /// <summary>Generate an equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.</summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is
        /// strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="resolution">Band diversity in octaves</param>
        /// <param name="targetGain">Target EQ level</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectGraph(float[] graph, float startFreq, float endFreq, EQCurve targetCurve, float targetGain,
            float resolution = 1 / 3f, float maxGain = 6) {
            Equalizer result = new Equalizer();
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2), bands = octaveRange / resolution + 1;
            int windowSize = graph.Length / (int)bands, windowEdge = windowSize / 2;
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize) {
                float centerFreq = (float)Math.Pow(10, startPow + powRange * pos), average = 0;
                int start = Math.Max(pos - windowEdge, 0), end = Math.Min(pos + windowEdge, graph.Length);
                for (int sample = start; sample < end; ++sample)
                    average += graph[sample];
                float addition = targetCurve.At(centerFreq) + targetGain - average / (end - start);
                if (addition <= maxGain)
                    result.bands.Add(new Band(centerFreq, addition));
            }
            result.bands.Reverse();
            return result;
        }

        /// <summary>Generate a precise equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.</summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is
        /// strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="targetCurve">Match the frequency response to this EQ curve</param>
        /// <param name="targetGain">Target EQ level</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer AutoCorrectGraph(float[] graph, float startFreq, float endFreq, EQCurve targetCurve, float targetGain,
            float maxGain = 6) {
            Equalizer result = new Equalizer();
            int length = graph.Length;
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / length;
            List<int> windowEdges = new List<int>(new int[] { 0 });
            for (int sample = 1, end = length - 1; sample < end; ++sample) {
                float lower = graph[sample - 1], Upper = graph[sample + 1];
                if ((lower < graph[sample] && Upper > graph[sample]) || (lower > graph[sample] && Upper < graph[sample]))
                    windowEdges.Add(sample);
            }
            for (int sample = 0, end = windowEdges.Count - 1; sample < end; ++sample) {
                int windowPos = windowEdges[sample];
                float frequency = (float)Math.Pow(10, startPow + powRange * windowPos), refGain = targetCurve.At(frequency) + targetGain;
                if (graph[windowPos] > refGain - maxGain)
                    result.bands.Add(new Band(frequency, refGain - graph[windowPos]));
            }
            return result;
        }
    }
}