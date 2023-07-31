using System;
using System.Globalization;
using System.IO;
using System.Text;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
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

            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {bands[band].Gain.ToString(culture)}";
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

            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {bands[band].Gain.ToString(culture)}";
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
        /// Get the average level between the <paramref name="minFreq"/> and <paramref name="maxFreq"/>.
        /// </summary>
        /// <returns></returns>
        public double GetAverageLevel(double minFreq, double maxFreq) {
            int i = 0, c = bands.Count;
            while (i < c && bands[i].Frequency < minFreq) {
                ++i;
            }
            double sum = 0;
            int n = 0;
            while (i < c && bands[i].Frequency <= maxFreq) {
                sum += Math.Pow(10, bands[i++].Gain * .05);
                ++n;
            }
            return 20 * Math.Log10(sum / n);
        }

        /// <summary>
        /// Get the average level between the rolloff points (-<paramref name="range"/> dB points)
        /// of the curve drawn by this <see cref="Equalizer"/>. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
        /// the limits of a frequency band that can't be overly distorted on the curve and shall work for regression line calculation.
        /// </summary>
        /// <returns></returns>
        public double GetAverageLevel(double range, double stableRangeStart, double stableRangeEnd) {
            (double minFreq, double maxFreq) = GetRolloffs(range, stableRangeStart, stableRangeEnd);
            return GetAverageLevel(minFreq, maxFreq);
        }

        /// <summary>
        /// Get a line in the form of slope * log10(x) + intercept that fits the curve drawn by this <see cref="Equalizer"/>.
        /// The regression line will only be calculated on the bands between <paramref name="startFreq"/> and <paramref name="endFreq"/>.
        /// </summary>
        public (double slope, double intercept) GetRegressionLine(double startFreq, double endFreq) {
            double freqSum = 0,
                freqSqSum = 0,
                gainSum = 0,
                mac = 0,
                n = 0;
            int i, c = bands.Count;
            for (i = 0; i < c && bands[i].Frequency < startFreq; i++) {
                // Skip the bands below the start freq
            }
            for (; i < c && bands[i].Frequency <= endFreq; i++) {
                double freq = Math.Log10(bands[i].Frequency);
                freqSum += freq;
                freqSqSum += freq * freq;
                gainSum += bands[i].Gain;
                mac += freq * bands[i].Gain;
                n++;
            }
            double slope = (n * mac - freqSum * gainSum) / (n * freqSqSum - freqSum * freqSum);
            return (slope, (gainSum - slope * freqSum) / n);
        }

        /// <summary>
        /// Get the rolloff points of the EQ by fitting a line on it and finding the first and last points
        /// with a given range in decibels below it. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
        /// the limits of a frequency band that can't be overly distorted on the curve and shall work for regression line calculation.
        /// </summary>
        public (double minFreq, double maxFreq) GetRolloffs(double range, double stableRangeStart, double stableRangeEnd) {
            (double m, double b) = GetRegressionLine(stableRangeStart, stableRangeEnd);
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
        /// End of a Dirac calibration file. We don't need these features for FIR.
        /// </summary>
        static readonly string[] diracFooter = { "HPSLOPEON", "0", "LPSLOPEON", "0", "HPCUTOFF", "10", "LPCUTOFF", "24000",
            "HPORDER", "4", "LPORDER", "4", "LOWLIMITHZ", "10", "HIGHLIMITHZ", "24000" };
    }
}