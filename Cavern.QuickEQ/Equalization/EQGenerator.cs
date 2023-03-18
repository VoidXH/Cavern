using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Equalizer generation functions.
    /// </summary>
    public static class EQGenerator {
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
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain,
            double resolution) => CorrectGraph(graph, startFreq, endFreq, targetCurve, targetGain, resolution, 6);

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
        public static Equalizer CorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve, float targetGain,
            double resolution, float maxGain) {
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
            return new Equalizer(bands);
        }

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
        public static Equalizer AutoCorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve,
            float targetGain) => AutoCorrectGraph(graph, startFreq, endFreq, targetCurve, targetGain, 6);

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
        public static Equalizer AutoCorrectGraph(float[] graph, double startFreq, double endFreq, EQCurve targetCurve,
            float targetGain, float maxGain) {
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
            return new Equalizer(bands);
        }

        /// <summary>
        /// Get the average gains of multiple equalizers. The averaging happens in linear space.
        /// </summary>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        public static Equalizer Average(params Equalizer[] sources) {
            double[] bands = new double[sources[0].Bands.Count];
            IReadOnlyList<Band> source = sources[0].Bands;
            for (int i = 0; i < bands.Length; i++) {
                bands[i] = Math.Pow(10, source[i].Gain * .05f);
            }
            for (int i = 1; i < sources.Length; i++) {
                source = sources[i].Bands;
                for (int j = 0; j < bands.Length; j++) {
                    bands[j] += Math.Pow(10, source[j].Gain * .05f);
                }
            }

            double div = 1.0 / sources.Length;
            List<Band> result = new List<Band>(bands.Length);
            for (int i = 0; i < bands.Length; i++) {
                result.Add(new Band(source[i].Frequency, 20 * Math.Log10(bands[i] * div)));
            }
            return new Equalizer(result);
        }

        /// <summary>
        /// Get the average gains of multiple equalizers, regardless of how many bands they have. The averaging happens in linear space.
        /// </summary>
        public static Equalizer AverageSafe(params Equalizer[] sources) {
            List<Band> bands = new List<Band>();
            double div = 1.0 / sources.Length;
            for (int i = 0; i < sources.Length; i++) {
                IReadOnlyList<Band> source = sources[i].Bands;
                for (int j = 0, c = source.Count; j < c; j++) {
                    double freq = source[j].Frequency,
                        gain = 0;
                    for (int other = 0; other < sources.Length; other++) {
                        gain += Math.Pow(10, sources[other][freq] * .05f);
                    }
                    bands.Add(new Band(freq, 20 * Math.Log10(gain * div)));
                }
            }

            bands.Sort();
            return new Equalizer(bands.Distinct().ToList());
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
            return new Equalizer(bands);
        }

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will not be applied, and the filter's length will be the default of 1024 samples.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate) =>
            eq.GetConvolution(sampleRate, 1024, 1, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will not be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length) =>
            eq.GetConvolution(sampleRate, length, 1, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// Additional gain will be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length, float gain) =>
            eq.GetConvolution(sampleRate, length, gain, null);

        /// <summary>
        /// Gets a zero-delay convolution filter with minimally sacrificed phase that results in this EQ when applied.
        /// The initial curve can be provided in Fourier-space.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public static float[] GetConvolution(this Equalizer eq, int sampleRate, int length, float gain, Complex[] initial) {
            length <<= 1;
            Complex[] filter = new Complex[length];
            if (initial == null) {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = gain; // FFT of DiracDelta(x)
                }
            } else {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = initial[i].Magnitude * gain;
                }
            }
            eq.Apply(filter, sampleRate);
            using (FFTCache cache = new FFTCache(length)) {
                Measurements.MinimumPhaseSpectrum(filter, cache);
                filter.InPlaceIFFT(cache);
            }
            return Measurements.GetRealPartHalf(filter);
        }

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will not be applied, and the filter's length will be the default of 1024 samples.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate) =>
            eq.GetLinearConvolution(sampleRate, 1024, 1, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will not be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length) =>
            eq.GetLinearConvolution(sampleRate, length, 1, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// Additional gain will be applied.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length, float gain) =>
            eq.GetLinearConvolution(sampleRate, length, gain, null);

        /// <summary>
        /// Gets a linear phase convolution filter that results in this EQ when applied.
        /// The initial curve can be provided in Fourier-space.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Sample rate of the target system the convolution filter could be used on</param>
        /// <param name="length">Length of the convolution filter in samples, must be a power of 2</param>
        /// <param name="gain">Signal voltage multiplier</param>
        /// <param name="initial">Custom initial spectrum to apply the EQ on - phases will be corrected, this is not convolved,
        /// and has to be twice the size of <paramref name="length"/></param>
        public static float[] GetLinearConvolution(this Equalizer eq, int sampleRate, int length, float gain, Complex[] initial) {
            Complex[] filter = new Complex[length];
            if (initial == null) {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = (i & 1) == 0 ? gain : -gain; // FFT of DiracDelta(x - length/2)
                }
            } else {
                for (int i = 0; i < length; ++i) {
                    filter[i].Real = initial[i].Magnitude * ((i & 1) == 0 ? gain : -gain);
                }
            }
            eq.Apply(filter, sampleRate);
            filter.InPlaceIFFT();
            return Measurements.GetRealPart(filter);
        }

        /// <summary>
        /// Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.
        /// The default of 2 octave smoothing will be used.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        public static PeakingEQ[] GetPeakingEQ(this Equalizer eq, int sampleRate) => eq.GetPeakingEQ(sampleRate, 2);

        /// <summary>
        /// Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        /// <param name="smoothing">Smooth out band spikes</param>
        public static PeakingEQ[] GetPeakingEQ(this Equalizer eq, int sampleRate, double smoothing) {
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
                result[end] = new PeakingEQ(sampleRate, freq,
                    QFactor.FromBandwidth(freq, (freq - bands[end - 1].Frequency) * 2) * qMul, bands[end].Gain * gainMul);
            } else if (result.Length == 1) {
                result[0] = new PeakingEQ(sampleRate, freq, .001f, bands[0].Gain);
            }
            return result;
        }

        /// <summary>
        /// Parse an Equalizer from a linear transfer function.
        /// </summary>
        public static Equalizer FromTransferFunction(Complex[] source, int sampleRate) {
            List<Band> bands = new List<Band>();
            double step = (double)sampleRate / (source.Length - 1);
            for (int entry = 0, end = source.Length >> 1; entry < end; entry++) {
                bands.Add(new Band(step * entry, 20 * Math.Log10(source[entry].Magnitude)));
            }
            return new Equalizer(bands);
        }

        /// <summary>
        /// Parse an Equalizer from a linear transfer function, but merge samples in logarithmic gaps (keep the octave range constant).
        /// </summary>
        public static unsafe Equalizer FromTransferFunctionOptimized(Complex[] source, int sampleRate) {
            List<Band> bands = new List<Band>();
            double step = (double)sampleRate / (source.Length - 1);
            fixed (Complex* pSource = source) {
                for (int entry = 2, end = source.Length >> 1; entry < end;) {
                    int merge = (int)Math.Log(entry, 2);
                    if (merge > end - entry) {
                        merge = end - entry;
                    }

                    float sum = 0;
                    for (Complex* i = pSource + entry, mergeUntil = i + merge; i != mergeUntil; i++) {
                        sum += (*i).Magnitude;
                    }
                    sum /= merge;

                    bands.Add(new Band(step * (entry + (merge - 1) * 0.5), 20 * Math.Log10(sum)));
                    entry += merge;
                }
            }
            return new Equalizer(bands);
        }

        /// <summary>
        /// Parse an Equalizer from a drawn graph.
        /// </summary>
        public static Equalizer FromGraph(float[] source, double startFreq, double endFreq) {
            List<Band> bands = new List<Band>();
            GraphUtils.ForEachLog(source, startFreq, endFreq, (double freq, ref float gain) => bands.Add(new Band(freq, gain)));
            return new Equalizer(bands);
        }

        /// <summary>
        /// Parse a calibration array where entries are in frequency-gain (dB) pairs.
        /// </summary>
        public static Equalizer FromCalibration(float[] source) {
            List<Band> bands = new List<Band>();
            for (int band = 0; band < source.Length; band += 2) {
                bands.Add(new Band(source[band], source[band + 1]));
            }
            bands.Sort();
            return new Equalizer(bands);
        }

        /// <summary>
        /// Parse a calibration text where each line is a frequency-gain (dB) pair.
        /// </summary>
        /// <param name="contents">Contents of the calibration file</param>
        public static Equalizer FromCalibration(string contents) => FromCalibration(contents.Split("\n"));

        /// <summary>
        /// Parse a calibration text where each line is a frequency-gain (dB) pair.
        /// </summary>
        /// <param name="lines">Lines of the calibration file</param>
        public static Equalizer FromCalibration(string[] lines) {
            List<Band> bands = new List<Band>();
            for (int line = 0; line < lines.Length; ++line) {
                string[] nums = lines[line].Trim().Split(' ', '\t');
                if (nums.Length > 1 && double.TryParse(nums[0].Replace(',', '.'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double freq) && double.TryParse(nums[1].Replace(',', '.'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double gain)) {
                    bands.Add(new Band(freq, gain));
                }
            }
            bands.Sort();
            return new Equalizer(bands);
        }

        /// <summary>
        /// Parse a calibration file where each line is a frequency-gain (dB) pair, and the lines are sorted ascending by frequency.
        /// </summary>
        /// <param name="path">Path to the calibration file</param>
        public static Equalizer FromCalibrationFile(string path) => FromCalibration(File.ReadAllLines(path));
    }
}