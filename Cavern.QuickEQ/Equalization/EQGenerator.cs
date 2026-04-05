using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Equalizer generation functions.
    /// </summary>
    public static partial class EQGenerator {
        /// <summary>
        /// Fade the <paramref name="low"/> frequencies into the <paramref name="high"/>s around the <paramref name="transitionFreq"/>.
        /// </summary>
        /// <param name="low">Curve to take the low frequency values of</param>
        /// <param name="high">Curve to take the high frequency values of, the frequencies must match the <paramref name="low"/></param>
        /// <param name="transitionFreq">The point where both curves contribute equally</param>
        /// <param name="transitionSpan">In octaves, the width of the transition region</param>
        public static Equalizer Fade(Equalizer low, Equalizer high, double transitionFreq, double transitionSpan) {
            List<Band> output = new List<Band>();
            double transitionRange = Math.Pow(2, transitionSpan * .5);
            (int startBand, int endBand) = low.GetBandLimits(transitionFreq / transitionRange, transitionFreq * transitionRange);
            if (startBand == -1) {
                return (Equalizer)high.Clone();
            }

            IReadOnlyList<Band> lowBands = low.Bands,
                highBands = high.Bands;
            for (int i = 0; i < startBand; i++) {
                output.Add(lowBands[i]);
            }
            for (int i = startBand; i < endBand; i++) {
                double ratio = QMath.LerpInverse(startBand, endBand, i);
                output.Add(new Band(lowBands[i].Frequency, QMath.Lerp(lowBands[i].Gain, highBands[i].Gain, ratio)));
            }
            for (int i = endBand, c = highBands.Count; i < c; i++) {
                output.Add(highBands[i]);
            }
            return new Equalizer(output, true);
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
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Parse a calibration text where each line is a frequency-gain (dB) pair.
        /// </summary>
        /// <param name="contents">Contents of the calibration file</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer FromCalibration(string contents) => FromCalibration(contents.Split("\n"));

        /// <summary>
        /// Parse a calibration text where each line is a frequency-gain (dB) pair.
        /// </summary>
        /// <param name="lines">Lines of the calibration file</param>
        public static Equalizer FromCalibration(string[] lines) {
            List<Band> bands = new List<Band>();
            for (int line = 0; line < lines.Length; ++line) {
                string[] nums = lines[line].Trim().Split(new[] { ' ', '\t' });
                if (nums.Length > 1 && QMath.TryParseDouble(nums[0], out double freq) && QMath.TryParseDouble(nums[1], out double gain)) {
                    bands.Add(new Band(freq, gain));
                }
            }
            bands.Sort();
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Parse a calibration file where each line is a frequency-gain (dB) pair, and the lines are sorted ascending by frequency.
        /// </summary>
        /// <param name="path">Path to the calibration file</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer FromCalibrationFile(string path) => FromCalibration(File.ReadAllLines(path));

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO to a Cavern <see cref="Equalizer"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer FromEqualizerAPO(string line) => FromEqualizerAPO(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO which was split at spaces to a Cavern <see cref="Equalizer"/>.
        /// </summary>
        public static Equalizer FromEqualizerAPO(string[] splitLine) {
            float[] toParse = new float[splitLine.Length - 1];
            for (int i = 1; i < splitLine.Length; i++) {
                if (splitLine[i][^1] == ';') {
                    toParse[i - 1] = QMath.ParseFloat(splitLine[i][..^1]);
                } else {
                    toParse[i - 1] = QMath.ParseFloat(splitLine[i]);
                }
            }
            return FromCalibration(toParse);
        }

        /// <summary>
        /// Parse an Equalizer from a drawn curve (linear data between 0 and <paramref name="sampleRate"/>/2 frequencies).
        /// </summary>
        public static Equalizer FromCurve(float[] source, int sampleRate) {
            List<Band> bands = new List<Band>();
            GraphUtils.ForEachLin(source, 0, sampleRate >> 1, (double freq, ref float gain) => bands.Add(new Band(freq, gain)));
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Parse an Equalizer from a drawn graph (logarithmic, band-limited data).
        /// </summary>
        public static Equalizer FromGraph(float[] source, double startFreq, double endFreq) {
            List<Band> bands = new List<Band>();
            GraphUtils.ForEachLog(source, startFreq, endFreq, (double freq, ref float gain) => bands.Add(new Band(freq, gain)));
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Parse <paramref name="lines"/> of <see cref="PeakingEQ"/>s between 20 Hz and 20 kHz with 1/12 octave precision.
        /// </summary>
        public static Equalizer FromPeakingEQFile(IEnumerable<string> lines) => FromPeakingEQFile(lines, 20, 20000, 1.0 / 12);

        /// <summary>
        /// Parse a file of <see cref="PeakingEQ"/>s between 20 Hz and 20 kHz with 1/12 octave precision.
        /// </summary>
        public static Equalizer FromPeakingEQFile(string path) => FromPeakingEQFile(path, 20, 20000, 1.0 / 12);

        /// <summary>
        /// Parse <paramref name="lines"/> of <see cref="PeakingEQ"/>s, but since they're infinite resolution, have a <paramref name="resolution"/> limit in octaves.
        /// </summary>
        public static Equalizer FromPeakingEQFile(IEnumerable<string> lines, double startFreq, double endFreq, double resolution) {
            PeakingEQ[] parsed = PeakingEqualizer.ParseEQFile(lines);
            return FromPeakingEQFile(parsed, startFreq, endFreq, resolution);
        }

        /// <summary>
        /// Parse a file of <see cref="PeakingEQ"/>s, but since they're infinite resolution, have a <paramref name="resolution"/> limit in octaves.
        /// </summary>
        public static Equalizer FromPeakingEQFile(string path, double startFreq, double endFreq, double resolution) {
            PeakingEQ[] parsed = PeakingEqualizer.ParseEQFile(path);
            return FromPeakingEQFile(parsed, startFreq, endFreq, resolution);
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
            return new Equalizer(bands, true);
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
            return new Equalizer(bands, true);
        }

        /// <summary>
        /// Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.
        /// The default of 2 octave smoothing will be used.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Parse a file of <see cref="PeakingEQ"/>s, but since they're infinite resolution, have a <paramref name="resolution"/> limit in octaves.
        /// </summary>
        static Equalizer FromPeakingEQFile(PeakingEQ[] source, double startFreq, double endFreq, double resolution) {
            if (source.Length == 0) {
                return new Equalizer();
            }

            FilterAnalyzer analyzer = new FilterAnalyzer(new ComplexFilter(source), source[0].SampleRate);
            return analyzer.ToEqualizer(startFreq, endFreq, resolution);
        }
    }
}
