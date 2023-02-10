using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

using static Cavern.QuickEQ.Windowing;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Apply a slope (in decibels, per octave) between two frequencies.
        /// </summary>
        public void AddSlope(double slope, double startFreq, double endFreq) {
            double logStart = Math.Log(startFreq),
                range = Math.Log(endFreq) - logStart;
            slope *= range * 3.32192809489f; // range / log(2)
            for (int i = 0, c = bands.Count; i < c; i++) {
                if (bands[i].Frequency > startFreq) {
                    if (bands[i].Frequency > endFreq) {
                        bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slope);
                    } else {
                        bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slope * (Math.Log(bands[i].Frequency) - logStart) / range);
                    }
                }
            }
        }

        /// <summary>
        /// Set this equalizer so if the <paramref name="other"/> is linear, this will be the difference from it.
        /// </summary>
        /// <remarks>Matching frequencies have to be guaranteed before calling this function with
        /// <see cref="HasTheSameFrequenciesAs(Equalizer)"/>.</remarks>
        public void AlignTo(Equalizer other) {
            List<Band> otherBands = other.bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                bands[i] -= otherBands[i].Gain;
            }
        }

        /// <summary>
        /// Shows the resulting frequency response if this EQ is applied.
        /// </summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, double startFreq, double endFreq) {
            float[] filter = Visualize(startFreq, endFreq, response.Length);
            for (int i = 0; i < response.Length; i++) {
                filter[i] += response[i];
            }
            return filter;
        }

        /// <summary>
        /// Apply this EQ on a frequency response.
        /// </summary>
        /// <param name="response">Frequency response to apply the EQ on</param>
        /// <param name="sampleRate">Sample rate where <paramref name="response"/> was generated</param>
        public void Apply(Complex[] response, int sampleRate) {
            int halfLength = response.Length / 2 + 1, nyquist = sampleRate / 2;
            float[] filter = VisualizeLinear(0, nyquist, halfLength);
            response[0] *= (float)Math.Pow(10, filter[0] * .05f);
            for (int i = 1; i < halfLength; i++) {
                response[i] *= (float)Math.Pow(10, filter[i] * .05f);
                response[^i] = new Complex(response[i].Real, -response[i].Imaginary);
            }
        }

        /// <summary>
        /// Save this EQ to a file in the standard curve/calibration format.
        /// </summary>
        /// <param name="path">Export path of the file</param>
        /// <param name="level">Gain at the center of the curve</param>
        /// <param name="header">Extra text to be added to the first line of the file</param>
        public void Export(string path, double level, string header = null) {
            int start = header != null ? 1 : 0, c = bands.Count;
            string[] calFile = new string[bands.Count + start];
            if (header != null) {
                calFile[0] = header;
            }

            double normalize = bands[c / 2].Gain - level;
            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {(bands[band].Gain - normalize).ToString(culture)}";
            }
            File.WriteAllLines(path, calFile);
        }

        /// <summary>
        /// Save this EQ to a file in Dirac's curve format.
        /// </summary>
        /// <param name="path">Export path of the file</param>
        /// <param name="level">Gain at the center of the curve</param>
        /// <param name="header">Extra text to be added to the first line of the file</param>
        public void ExportToDirac(string path, double level, string header = null) {
            int start = header != null ? 2 : 1, c = bands.Count;
            string[] calFile = new string[1 + diracFooter.Length + bands.Count + start];
            if (header != null) {
                calFile[0] = '#' + header;
            }
            calFile[start - 1] = "BREAKPOINTS";

            double normalize = bands[c / 2].Gain - level;
            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {(bands[band].Gain - normalize).ToString(culture)}";
            }

            Array.Copy(diracFooter, 0, calFile, start + c, diracFooter.Length);
            File.WriteAllLines(path, calFile);
        }

        /// <summary>
        /// Get a line in an Equalizer APO configuration file that applies this EQ.
        /// </summary>
        /// <returns></returns>
        public string ExportToEqualizerAPO() {
            StringBuilder result = new StringBuilder("GraphicEQ:");
            int band = 0, c = bands.Count;
            while (band < c) {
                result.Append(' ').Append(bands[band].Frequency.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(bands[band].Gain.ToString(CultureInfo.InvariantCulture));
                if (++band != c) {
                    result.Append(';');
                } else {
                    break;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Get the average level between the rolloff points (-<paramref name="range"/> dB points)
        /// of the curve drawn by this <see cref="Equalizer"/>.
        /// </summary>
        /// <returns></returns>
        public double GetAverageLevel(double range) {
            (double minFreq, double maxFreq) = GetRolloffs(range);
            int i = 0, c = bands.Count;
            while (i < c && bands[i].Frequency < minFreq) {
                ++i;
            }
            double sum = 0;
            int n = 0;
            while (i < c && bands[i].Frequency < maxFreq) {
                sum += Math.Pow(10, bands[i++].Gain * .05);
                ++n;
            }
            return 20 * Math.Log10(sum / n);
        }

        /// <summary>
        /// Get a line in the form of m*log10(x)+b that fits the curve drawn by this <see cref="Equalizer"/>.
        /// </summary>
        public (double m, double b) GetRegressionLine() {
            double[] logFreqs = new double[bands.Count];
            double avgGain = 0;
            for (int i = 0; i < logFreqs.Length; i++) {
                logFreqs[i] = Math.Log10(bands[i].Frequency);
                avgGain += bands[i].Gain;
            }
            double avgFreq = QMath.Average(logFreqs);
            avgGain /= logFreqs.Length;

            double mNum = 0, mDen = 0;
            for (int i = 0; i < logFreqs.Length; i++) {
                double freqDiff = logFreqs[i] - avgFreq;
                mNum += freqDiff * (bands[i].Gain - avgGain);
                mDen += freqDiff * freqDiff;
            }
            double m = mNum / mDen;
            return (m, avgGain - m * avgFreq);
        }

        /// <summary>
        /// Get the rolloff points of the EQ by fitting a line on it and finding the first and last points
        /// with a given range in decibels below it.
        /// </summary>
        public (double minFreq, double maxFreq) GetRolloffs(double range) {
            (double m, double b) = GetRegressionLine();
            int first = 0, last = bands.Count - 1;
            while (first < last && bands[first].Gain + range < m * Math.Log10(bands[first].Frequency) + b) {
                ++first;
            }
            while (last > first && bands[last].Gain + range < m * Math.Log10(bands[last].Frequency) + b) {
                --last;
            }
            return (bands[first].Frequency, bands[last].Frequency);
        }

        /// <summary>
        /// Limit the application range of the EQ.
        /// </summary>
        /// <param name="startFreq">Bottom cutoff frequency</param>
        /// <param name="endFreq">Top cutoff frequency</param>
        /// <param name="targetCurve">If set, the cuts will conform to this curve</param>
        public void Limit(double startFreq, double endFreq, EQCurve targetCurve = null) {
            int c = bands.Count;
            for (int i = 0; i < c; i++) {
                if (bands[i].Frequency > startFreq) {
                    if (i != 0) {
                        double atStart = targetCurve != null ? targetCurve[startFreq] : this[startFreq];
                        bands.RemoveRange(0, i);
                        AddBand(new Band(startFreq, atStart));
                        c -= i;
                    }
                    break;
                }
            }

            for (int i = c - 1; i >= 0; i--) {
                if (bands[i].Frequency < endFreq) {
                    if (i + 1 != c) {
                        double atEnd = targetCurve != null ? targetCurve[endFreq] : this[endFreq];
                        bands.RemoveRange(i + 1, c - i - 1);
                        bands.Add(new Band(endFreq, atEnd));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Apply smoothing on this <see cref="Equalizer"/> with a window of a given octave.
        /// </summary>
        public void Smooth(double octaves) {
            int smoothFrom = 0, smoothTo = 0;
            double multipleTo = Math.Pow(2, octaves), multipleFrom = 1 / multipleTo;
            double[] result = new double[bands.Count];
            for (int i = 0; i < result.Length; i++) {
                double minFreq = bands[i].Frequency * multipleFrom,
                    maxFreq = bands[i].Frequency * multipleTo;
                while (smoothFrom < result.Length && bands[smoothFrom].Frequency < minFreq) {
                    ++smoothFrom;
                }
                while (smoothTo < result.Length && bands[smoothTo].Frequency < maxFreq) {
                    ++smoothTo;
                }
                result[i] = Math.Pow(10, bands[i].Gain * .05);
                if (smoothFrom != smoothTo) {
                    for (int j = smoothFrom + 1; j < smoothTo; j++) {
                        result[i] += Math.Pow(10, bands[j].Gain * .05);
                    }
                    result[i] = 20 * Math.Log10(result[i] / (smoothTo - smoothFrom));
                }
            }
            for (int i = 0; i < result.Length; i++) {
                bands[i] = new Band(bands[i].Frequency, result[i]);
            }
        }

        /// <summary>
        /// Shows the EQ curve in a logarithmically scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0, nextBand = 0, prevBand = 0; i < length; i++) {
                while (nextBand != bandCount && bands[nextBand].Frequency < startFreq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[i] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, startFreq));
                } else {
                    result[i] = (float)bands[prevBand].Gain;
                }
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>
        /// Shows the EQ curve in a linearly scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] VisualizeLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0, nextBand = 0, prevBand = 0; entry < length; entry++) {
                double freq = startFreq + step * entry;
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[entry] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                } else {
                    result[entry] = (float)bands[prevBand].Gain;
                }
            }
            return result;
        }

        /// <summary>
        /// Add windowing on the right of the curve. Windowing is applied logarithmically.
        /// </summary>
        public void Window(Window right, double startFreq, double endFreq) => ApplyWindow(bands, right, startFreq, endFreq);

        /// <summary>
        /// End of a Dirac calibration file. We don't need these features for FIR.
        /// </summary>
        static readonly string[] diracFooter = { "HPSLOPEON", "0", "LPSLOPEON", "0", "HPCUTOFF", "10", "LPCUTOFF", "24000",
            "HPORDER", "4", "LPORDER", "4", "LOWLIMITHZ", "10", "HIGHLIMITHZ", "24000" };
    }
}