using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.EQCurves;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>Equalizer generation functions.</summary>
    public static class EQGenerator {
        static NumberFormatInfo NumberFormat {
            get {
                if (numberFormat == null)
                    numberFormat = new NumberFormatInfo { NumberDecimalSeparator = "," };
                return numberFormat;
            }
        }
        static NumberFormatInfo numberFormat;

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
            List<Band> bands = new List<Band>();
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(endFreq) - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2);
            int windowSize = graph.Length / (int)(octaveRange / resolution + 1), windowEdge = windowSize / 2;
            float[] refGain = targetCurve.GenerateLogCurve(graph.Length, startFreq, endFreq);
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize) {
                float centerFreq = (float)Math.Pow(10, startPow + powRange * pos), average = 0;
                int start = Math.Max(pos - windowEdge, 0), end = Math.Min(pos + windowEdge, graph.Length);
                for (int sample = start; sample < end; ++sample)
                    average += graph[sample];
                float addition = refGain[pos] + targetGain - average / (end - start);
                if (addition <= maxGain)
                    bands.Add(new Band(centerFreq, addition));
            }
            bands.Reverse();
            return new Equalizer(bands);
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
            List<Band> bands = new List<Band>();
            double startPow = Math.Log10(startFreq), endPow = Math.Log10(endFreq), powRange = (endPow - startPow) / graph.Length;
            List<int> windowEdges = new List<int>(new int[] { 0 });
            for (int sample = 1, end = graph.Length - 1; sample < end; ++sample)
                if ((graph[sample - 1] < graph[sample] && graph[sample + 1] > graph[sample]) ||
                    (graph[sample - 1] > graph[sample] && graph[sample + 1] < graph[sample]))
                    windowEdges.Add(sample);
            float[] refGain = targetCurve.GenerateLogCurve(graph.Length, startFreq, endFreq, targetGain);
            for (int sample = 0, end = windowEdges.Count - 1; sample < end; ++sample) {
                int windowPos = windowEdges[sample];
                if (graph[windowPos] > refGain[windowPos] - maxGain)
                    bands.Add(new Band((float)Math.Pow(10, startPow + powRange * windowPos), refGain[windowPos] - graph[windowPos]));
            }
            return new Equalizer(bands);
        }

        /// <summary>Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.</summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length = 1024, float gain = 1, Complex[] initial = null) {
            length <<= 1;
            Complex[] filter = new Complex[length];
            if (initial == null)
                for (int i = 0; i < length; ++i)
                    filter[i].Real = gain; // FFT of DiracDelta(x)
            else
                for (int i = 0; i < length; ++i)
                    filter[i].Real = initial[i].Magnitude * gain;
            eq.Apply(filter, sampleRate);
            FFTCache cache = new FFTCache(length);
            Measurements.MinimumPhaseSpectrum(filter, cache);
            Measurements.InPlaceIFFT(filter, cache);
            return Measurements.GetRealPartHalf(filter);
        }

        /// <summary>Gets a linear phase convolution filter that results in this EQ when applied.</summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length = 1024, float gain = 1, Complex[] initial = null) {
            Complex[] filter = new Complex[length];
            if (initial == null)
                for (int i = 0; i < length; ++i)
                    filter[i].Real = i % 2 == 0 ? gain : -gain; // FFT of DiracDelta(x - length/2)
            else
                for (int i = 0; i < length; ++i)
                    filter[i].Real = initial[i].Magnitude * (i % 2 == 0 ? gain : -gain);
            eq.Apply(filter, sampleRate);
            Measurements.InPlaceIFFT(filter);
            return Measurements.GetRealPart(filter);
        }

        /// <summary>Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.</summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        /// <param name="smoothing">Smooth out band spikes</param>
        public static PeakingEQ[] GetPeakingEQ(this Equalizer eq, int sampleRate, double smoothing = 2) {
            IReadOnlyList<Band> bands = eq.Bands;
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

        /// <summary>Parse a calibration text where each line is a frequency-gain (dB) pair,
        /// and the lines are sorted ascending by frequency.</summary>
        /// <param name="lines">Lines of the calibration file</param>
        public static Equalizer FromCalibration(string[] lines) {
            List<Band> bands = new List<Band>();
            for (int line = 0; line < lines.Length; ++line) {
                string[] nums = lines[line].Trim().Split(' ', '\t');
                if (nums.Length > 1 && double.TryParse(nums[0].Replace(',', '.'), NumberStyles.Any, NumberFormat, out double freq) &&
                    double.TryParse(nums[1].Replace(',', '.'), NumberStyles.Any, NumberFormat, out double gain))
                    bands.Add(new Band(freq, gain));
            }
            bands.Sort();
            return new Equalizer(bands);
        }

        /// <summary>Parse a calibration file where each line is a frequency-gain (dB) pair,
        /// and the lines are sorted ascending by frequency.</summary>
        /// <param name="path">Path to the calibration file</param>
        public static Equalizer FromCalibrationFile(string path) => FromCalibration(File.ReadAllLines(path));
    }
}