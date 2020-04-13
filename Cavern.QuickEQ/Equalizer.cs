using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Equalizer data collector and exporter.</summary>
    public sealed class Equalizer {
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
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                subsonicFilter = value;
            }
        }
        bool subsonicFilter = false;

        /// <summary>The highest gain in this EQ.</summary>
        public double PeakGain { get; private set; }

        void RecalculatePeakGain() {
            PeakGain = bands[0].Gain;
            for (int band = 1, count = bands.Count; band < count; ++band)
                if (PeakGain < bands[band].Gain)
                    PeakGain = bands[band].Gain;
        }

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band newBand) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            if (bands.Count == 0 || PeakGain < newBand.Gain)
                PeakGain = newBand.Gain;
            bands.Add(newBand);
            bands.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
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
            else if (PeakGain == removable.Gain)
                RecalculatePeakGain();
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

        /// <summary>Visualize with a custom enumerator.</summary>
        /// <param name="enumerator">Enumerates values by the frequency axis, with a given scale</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        float[] Visualize(Action<float[], double, double, GraphUtils.FrequencyFunction<float>> enumerator,
            double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count, nextBand = 0, prevBand = 0;
            if (bandCount == 0)
                return result;
            enumerator(result, startFreq, endFreq, (double freq, ref float value) => {
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    value = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                else
                    value = (float)bands[prevBand].Gain;
            });
            return result;
        }

        /// <summary>Shows the EQ curve in a logarithmically scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) => Visualize(GraphUtils.ForEachLog, startFreq, endFreq, length);

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

        /// <summary>Apply this EQ on a frequency response.</summary>
        /// <param name="response">Frequency response to apply the EQ on</param>
        /// <param name="sampleRate">Sample rate where <paramref name="response"/> was generated</param>
        public void Apply(Complex[] response, int sampleRate) {
            int halfLength = response.Length / 2 + 1, nyquist = sampleRate / 2;
            float[] filter = Visualize(GraphUtils.ForEachLin, 0, nyquist, halfLength);
            response[0] *= (float)Math.Pow(10, filter[0] * .05f);
            for (int i = 1, end = response.Length; i < halfLength; ++i) {
                response[i] *= (float)Math.Pow(10, filter[i] * .05f);
                response[end - i] = new Complex(response[i].Real, -response[i].Imaginary);
            }
        }

        /// <summary>Minimizes the phase of a spectrum.</summary>
        /// <remarks>This function does not handle zeros in the spectrum. Make sure there is a threshold before using this function.</remarks>
        void MinimumPhaseSpectrum(Complex[] response, FFTCache cache = null) {
            if (cache == null)
                cache = new FFTCache(response.Length);
            int halfLength = response.Length / 2;
            for (int i = 0; i < response.Length; ++i) {
                response[i].Real = (float)Math.Log(response[i].Real);
                response[i].Imaginary = 0;
            }
            Measurements.InPlaceIFFT(response, cache);
            for (int i = 1; i < halfLength; ++i) {
                response[i].Real += response[response.Length - i].Real;
                response[i].Imaginary -= response[response.Length - i].Imaginary;
                response[response.Length - i].Real = 0;
                response[response.Length - i].Imaginary = 0;
            }
            response[halfLength].Imaginary = -response[halfLength].Imaginary;
            Measurements.InPlaceFFT(response, cache);
            for (int i = 0; i < response.Length; ++i) {
                double exp = Math.Exp(response[i].Real);
                response[i].Real = (float)(exp * Math.Cos(response[i].Imaginary));
                response[i].Imaginary = (float)(exp * Math.Sin(response[i].Imaginary));
            }
        }

        /// <summary>Gets a convolution filter that results in this EQ when applied.</summary>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        public float[] GetConvolution(int sampleRate, int length = 1024, float gain = 1) {
            int processedLength = length * 2;
            Complex[] filter = new Complex[processedLength];
            for (int i = 0; i < processedLength; ++i)
                filter[i].Real = gain; // FFT of DiracDelta(x)
            Apply(filter, sampleRate);
            FFTCache cache = new FFTCache(processedLength);
            MinimumPhaseSpectrum(filter);
            Measurements.InPlaceIFFT(filter, cache);
            return Measurements.GetRealPartHalf(filter);
        }

        /// <summary>Create a peaking EQ filter set to approximate the drawn EQ curve.</summary>
        /// <param name="sampleRate">Target system sample rate</param>
        /// <param name="smoothing">Smooth out band spikes</param>
        public PeakingEQ[] GetPeakingEQ(int sampleRate, double smoothing = 2) {
            PeakingEQ[] result = new PeakingEQ[bands.Count];
            double freq = bands[0].Frequency, gainMul = 1 / Math.Pow(smoothing, QFactor.reference), qMul = Math.PI / smoothing;
            if (result.Length > 1) {
                result[0] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, (bands[1].Frequency - freq) * 2) * qMul,
                    bands[0].Gain * gainMul);
                int end = result.Length - 1;
                for (int band = 1; band < end; ++band) {
                    freq = bands[band].Frequency;
                    result[band] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, bands[band - 1].Frequency, bands[band + 1].Frequency)
                        * qMul, bands[band].Gain * gainMul);
                }
                freq = bands[end].Frequency;
                result[end] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, (freq - bands[end - 1].Frequency) * 2) * qMul,
                    bands[end].Gain * gainMul);
            } else if (result.Length == 1)
                result[0] = new PeakingEQ(sampleRate, freq, .001f, bands[0].Gain);
            return result;
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
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain,
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
            result.RecalculatePeakGain();
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
        public static Equalizer AutoCorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain,
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
            result.RecalculatePeakGain();
            return result;
        }

        /// <summary>Parse a calibration text where each line is a frequency-gain (dB) pair, and the lines are sorted ascending by frequency.</summary>
        /// <param name="lines">Lines of the calibration file</param>
        public static Equalizer FromCalibration(string[] lines) {
            Equalizer result = new Equalizer();
            NumberFormatInfo format = new NumberFormatInfo { NumberDecimalSeparator = "," };
            for (int line = 0; line < lines.Length; ++line) {
                string[] nums = lines[line].Split(' ', '\t');
                if (float.TryParse(nums[0].Replace(',', '.'), NumberStyles.Any, format, out float freq) &&
                    float.TryParse(nums[nums.Length - 1].Replace(',', '.'), NumberStyles.Any, format, out float gain))
                    result.bands.Add(new Band(freq, gain));
            }
            return result;
        }

        /// <summary>Parse a calibration file where each line is a frequency-gain (dB) pair, and the lines are sorted ascending by frequency.</summary>
        /// <param name="path">Path to the calibration file</param>
        public static Equalizer FromCalibrationFile(string path) => FromCalibration(File.ReadAllLines(path));
    }
}