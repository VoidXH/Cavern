using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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

        /// <summary>Equalizer data collector and exporter.</summary>
        public Equalizer() { }

        /// <summary>Equalizer data collector and exporter from a previously created set of bands.</summary>
        internal Equalizer(List<Band> bands) {
            bands.Sort();
            this.bands = bands;
        }

        /// <summary>Gets the gain at a given frequency.</summary>
        public double this[double frequency] {
            get {
                int bandCount = bands.Count;
                if (bandCount == 0)
                    return 0;
                int nextBand = 0, prevBand = 0;
                while (nextBand != bandCount && bands[nextBand].Frequency < frequency) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    return QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, frequency));
                return bands[prevBand].Gain;
            }
        }

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
            if (bands.Count == 0) {
                PeakGain = 0;
                return;
            }
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
            bands.Sort();
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

        /// <summary>Remove multiple bands from the EQ.</summary>
        /// <param name="first">First band</param>
        /// <param name="count">Number of bands to remove starting with <paramref name="first"/></param>
        public void RemoveBands(Band first, int count) {
            bool subFiltered = subsonicFilter;
            if (subFiltered)
                SubsonicFilter = false;
            int start = bands.FindIndex(band => band.Equals(first));
            bool recalculatePeak = false;
            for (int i = 0; i < count; ++i) {
                if (bands[start + i].Gain == PeakGain) {
                    recalculatePeak = true;
                    break;
                }
            }
            bands.RemoveRange(start, count);
            if (bands.Count == 0)
                PeakGain = 0;
            else if (recalculatePeak)
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

        /// <summary>Shows the EQ curve in a linearly scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] VisualizeLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0)
                return result;
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0, nextBand = 0, prevBand = 0; entry < length; ++entry) {
                double freq = startFreq + step * entry;
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    result[entry] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                else
                    result[entry] = (float)bands[prevBand].Gain;
            }
            return result;
        }

        /// <summary>Gets the corresponding frequencies for <see cref="VisualizeLinear(double, double, int)"/>.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public static float[] FrequenciesLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0; entry < length; ++entry)
                result[entry] = (float)(startFreq + step * entry);
            return result;
        }

        /// <summary>Shows the EQ curve in a logarithmically scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0)
                return result;
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0, nextBand = 0, prevBand = 0; i < length; ++i) {
                while (nextBand != bandCount && bands[nextBand].Frequency < startFreq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    result[i] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, startFreq));
                else
                    result[i] = (float)bands[prevBand].Gain;
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>Gets the corresponding frequencies for <see cref="Visualize(double, double, int)"/>.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public static float[] Frequencies(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0; i < length; ++i) {
                result[i] = (float)startFreq;
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>Shows the resulting frequency response if this EQ is applied.</summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, double startFreq, double endFreq) {
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
            float[] filter = VisualizeLinear(0, nyquist, halfLength);
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

        /// <summary>Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.</summary>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initialSpectrum">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public float[] GetConvolution(int sampleRate, int length = 1024, float gain = 1, Complex[] initialSpectrum = null) {
            length <<= 1;
            Complex[] filter = new Complex[length];
            if (initialSpectrum == null)
                for (int i = 0; i < length; ++i)
                    filter[i].Real = gain; // FFT of DiracDelta(x)
            else
                for (int i = 0; i < length; ++i)
                    filter[i].Real = initialSpectrum[i].Magnitude * gain;
            Apply(filter, sampleRate);
            FFTCache cache = new FFTCache(length);
            MinimumPhaseSpectrum(filter, cache);
            Measurements.InPlaceIFFT(filter, cache);
            return Measurements.GetRealPartHalf(filter);
        }

        /// <summary>Gets a linear phase convolution filter that results in this EQ when applied.</summary>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initialSpectrum">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public float[] GetLinearConvolution(int sampleRate, int length = 1024, float gain = 1, Complex[] initialSpectrum = null) {
            Complex[] filter = new Complex[length];
            if (initialSpectrum == null)
                for (int i = 0; i < length; ++i)
                    filter[i].Real = i % 2 == 0 ? gain : -gain; // FFT of DiracDelta(x - length/2)
            else
                for (int i = 0; i < length; ++i)
                    filter[i].Real = initialSpectrum[i].Magnitude * (i % 2 == 0 ? gain : -gain);
            Apply(filter, sampleRate);
            Measurements.InPlaceIFFT(filter);
            return Measurements.GetRealPart(filter);
        }

        /// <summary>Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.</summary>
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
                    result[band] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, bands[band - 1].Frequency,
                        bands[band + 1].Frequency) * qMul, bands[band].Gain * gainMul);
                }
                freq = bands[end].Frequency;
                result[end] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, (freq - bands[end - 1].Frequency) * 2) * qMul,
                    bands[end].Gain * gainMul);
            } else if (result.Length == 1)
                result[0] = new PeakingEQ(sampleRate, freq, .001f, bands[0].Gain);
            return result;
        }

        /// <summary>Merge this Equalizer with another, summing their gains.</summary>
        public Equalizer Merge(Equalizer with) {
            List<Band> output = new List<Band>();
            for (int band = 0, bandc = bands.Count; band < bandc; ++ band)
                output.Add(new Band(bands[band].Frequency, bands[band].Gain + with[bands[band].Frequency]));
            for (int band = 0, bandc = with.bands.Count; band < bandc; ++band)
                output.Add(new Band(with.bands[band].Frequency, with.bands[band].Gain + this[with.bands[band].Frequency]));
            return new Equalizer(output.Distinct().ToList());
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
            double resolution = 1 / 3f, float maxGain = 6) {
            Equalizer result = new Equalizer();
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(endFreq) - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2), bands = octaveRange / resolution + 1;
            int windowSize = graph.Length / (int)bands, windowEdge = windowSize / 2;
            float[] refGain = targetCurve.GenerateLogCurve(graph.Length, startFreq, endFreq);
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize) {
                float centerFreq = (float)Math.Pow(10, startPow + powRange * pos), average = 0;
                int start = Math.Max(pos - windowEdge, 0), end = Math.Min(pos + windowEdge, graph.Length);
                for (int sample = start; sample < end; ++sample)
                    average += graph[sample];
                float addition = refGain[pos] + targetGain - average / (end - start);
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
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / graph.Length;
            List<int> windowEdges = new List<int>(new int[] { 0 });
            for (int sample = 1, end = graph.Length - 1; sample < end; ++sample)
                if ((graph[sample - 1] < graph[sample] && graph[sample + 1] > graph[sample]) ||
                    (graph[sample - 1] > graph[sample] && graph[sample + 1] < graph[sample]))
                        windowEdges.Add(sample);
            float[] refGain = targetCurve.GenerateLogCurve(graph.Length, startFreq, endFreq, targetGain);
            for (int sample = 0, end = windowEdges.Count - 1; sample < end; ++sample) {
                int windowPos = windowEdges[sample];
                float frequency = (float)Math.Pow(10, startPow + powRange * windowPos);
                if (graph[windowPos] > refGain[windowPos] - maxGain)
                    result.bands.Add(new Band(frequency, refGain[windowPos] - graph[windowPos]));
            }
            result.RecalculatePeakGain();
            return result;
        }

        /// <summary>Parse a calibration text where each line is a frequency-gain (dB) pair,
        /// and the lines are sorted ascending by frequency.</summary>
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

        /// <summary>Parse a calibration file where each line is a frequency-gain (dB) pair,
        /// and the lines are sorted ascending by frequency.</summary>
        /// <param name="path">Path to the calibration file</param>
        public static Equalizer FromCalibrationFile(string path) => FromCalibration(File.ReadAllLines(path));
    }
}