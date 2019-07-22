using Cavern.Utilities;
using System;
using System.Collections.Generic;

namespace Cavern.QuickEQ {
    /// <summary>Equalizer data collector and exporter.</summary>
    public class Equalizer {
        /// <summary>A single equalizer band.</summary>
        public struct Band {
            /// <summary>Position of the band.</summary>
            public double Frequency;
            /// <summary>Gain at <see cref="Frequency"/> in dB.</summary>
            public double Gain;

            /// <summary>EQ band constructor.</summary>
            public Band(double frequency, double gain) {
                Frequency = frequency;
                Gain = gain;
            }
        }

        /// <summary>Bands that make up this equalizer.</summary>
        public IReadOnlyList<Band> Bands => bands;
        List<Band> bands = new List<Band>();

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
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                subsonicFilter = value;
            }
        }
        bool subsonicFilter = false;

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band NewBand) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            bands.Add(NewBand);
            bands.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
            if (subFiltered)
                SubsonicFilter = true;
        }

        /// <summary>Remove a band from the EQ.</summary>
        public void RemoveBand(Band Removable) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            bands.Remove(Removable);
            if (subFiltered)
                SubsonicFilter = true;
        }

        /// <summary>Reset this EQ.</summary>
        public void ClearBands() {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
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
                while (nextBand != bandCount && bands[nextBand].Frequency < freq)
                    prevBand = ++nextBand - 1;
                if (nextBand != bandCount && nextBand != 0)
                    value = (float)Utils.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        Utils.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                else
                    value = (float)bands[prevBand].Gain;
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
        /// <param name="referenceCurve">Match the frequency response to this logarithmic curve of any length, one value means a flat response</param>
        /// <param name="resolution">Band diversity in octaves</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectGraph(float[] graph, float startFreq, float endFreq, float[] referenceCurve, float resolution = 1 / 3f, float maxGain = 6) {
            Equalizer result = new Equalizer();
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2), bands = octaveRange / resolution + 1,
                refPositioner = referenceCurve.Length / (double)graph.Length;
            int windowSize = graph.Length / (int)bands, windowEdge = windowSize / 2;
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize) {
                int refPos = (int)(pos * refPositioner);
                float centerFreq = (float)Math.Pow(10, startPow + powRange * pos), average = 0;
                int start = Math.Max(pos - windowEdge, 0), end = Math.Min(pos + windowEdge, graph.Length);
                for (int sample = start; sample < end; ++sample)
                    average += graph[sample];
                float addition = referenceCurve[refPos] - average / (end - start);
                if (addition <= maxGain)
                    result.bands.Add(new Band(centerFreq, addition));
            }
            result.bands.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
            return result;
        }

        /// <summary>Generate a precise equalizer setting to flatten the processed response of
        /// <see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/>.</summary>
        /// <param name="graph">Graph to equalize, a pre-applied smoothing (<see cref="GraphUtils.SmoothGraph(float[], float, float, float)"/> is
        /// strongly recommended</param>
        /// <param name="startFreq">Frequency at the beginning of the graph</param>
        /// <param name="endFreq">Frequency at the end of the graph</param>
        /// <param name="referenceCurve">Match the frequency response to this logarithmic curve of any length, one value means a flat response</param>
        /// <param name="maxGain">Maximum gain of any generated band</param>
        public static Equalizer AutoCorrectGraph(float[] graph, float startFreq, float endFreq, float[] referenceCurve, float maxGain = 6) {
            Equalizer result = new Equalizer();
            int length = graph.Length;
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / length,
                refPositioner = referenceCurve.Length / (double)length;
            List<int> windowEdges = new List<int>(new int[] { 0 });
            for (int sample = 1, End = length - 1; sample < End; ++sample) {
                float lower = graph[sample - 1], Upper = graph[sample + 1];
                if ((lower < graph[sample] && Upper > graph[sample]) || (lower > graph[sample] && Upper < graph[sample]))
                    windowEdges.Add(sample);
            }
            for (int sample = 0, End = windowEdges.Count - 1; sample < End; ++sample) {
                int windowPos = windowEdges[sample];
                float refGain = referenceCurve[(int)(windowPos * refPositioner)];
                if (graph[windowPos] > refGain - maxGain)
                    result.bands.Add(new Band((float)Math.Pow(10, startPow + powRange * windowPos), refGain - graph[windowPos]));
            }
            result.bands.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
            return result;
        }
    }
}