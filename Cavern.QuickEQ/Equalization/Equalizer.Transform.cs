using System.Collections.Generic;
using System;

using Cavern.QuickEQ.EQCurves;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Apply a slope (in decibels, per octave) between two frequencies.
        /// </summary>
        public void AddSlope(double slope, double startFreq, double endFreq) {
            double logStart = Math.Log(startFreq),
                range = Math.Log(endFreq) - logStart;
            slope *= range * 3.32192809489f; // range / log(2)
            for (int i = GetFirstBand(startFreq), c = bands.Count; i < c; i++) {
                if (bands[i].Frequency > endFreq) {
                    bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slope);
                } else {
                    bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slope * (Math.Log(bands[i].Frequency) - logStart) / range);
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
        /// Decrease the number of bands to this number at max. The function guarantees that a constant range
        /// between old bands is kept.
        /// </summary>
        public void Downsample(int numberOfBands) {
            int range = bands.Count / numberOfBands;
            if (range == 0) {
                return;
            }

            List<Band> newBands = new List<Band>();
            for (int i = 0, c = bands.Count; i < c; i++) {
                if (i % range == 0) {
                    newBands.Add(bands[i]);
                }
            }
            bands.Clear();
            bands.AddRange(newBands);
            RecalculatePeakGain();
        }

        /// <summary>
        /// Limit the application range of the EQ.
        /// </summary>
        /// <param name="startFreq">Bottom cutoff frequency</param>
        /// <param name="endFreq">Top cutoff frequency</param>
        /// <param name="targetCurve">If set, the cuts will conform to this curve</param>
        public void Limit(double startFreq, double endFreq, EQCurve targetCurve = null) {
            int i = GetFirstBand(startFreq),
                c = bands.Count;
            if (i > 0) {
                double atStart = targetCurve != null ? targetCurve[startFreq] : this[startFreq];
                bands.RemoveRange(0, i);
                AddBand(new Band(startFreq, atStart));
                c -= i;
            }

            for (i = c - 1; i >= 0; i--) {
                if (bands[i].Frequency < endFreq) {
                    if (i + 1 != c) {
                        double atEnd = targetCurve != null ? targetCurve[endFreq] : this[endFreq];
                        bands.RemoveRange(i + 1, c - i);
                        bands.Add(new Band(endFreq, atEnd));
                    }
                    break;
                }
            }
            RecalculatePeakGain();
        }

        /// <summary>
        /// Make sure the EQ won't go over the desired <paramref name="peak"/>.
        /// </summary>
        public void LimitPeaks(double peak) {
            for (int i = 0, c = bands.Count; i < c; i++) {
                if (bands[i].Gain > peak) {
                    bands[i] = new Band(bands[i].Frequency, peak);
                }
            }
            if (PeakGain > peak) {
                PeakGain = peak;
            }
        }

        /// <summary>
        /// Make sure that in the given range, all subsequent gains are smaller.
        /// </summary>
        public void MonotonousDecrease(double startFreq, double endFreq) {
            int last = GetFirstBand(endFreq);
            if (last == -1) {
                last = bands.Count - 1;
            }
            for (int first = GetFirstBand(startFreq); first < last; last--) {
                if (bands[last - 1].Gain < bands[last].Gain) {
                    bands[last - 1] = new Band(bands[last - 1].Frequency, bands[last].Gain);
                }
            }
        }

        /// <summary>
        /// Make sure that in the given range, all subsequent gains are larger.
        /// </summary>
        public void MonotonousIncrease(double startFreq, double endFreq) {
            for (int i = Math.Max(GetFirstBand(startFreq), 1), c = bands.Count; i < c; i++) {
                if (bands[i].Gain > endFreq) {
                    break;
                }

                if (bands[i - 1].Gain > bands[i].Gain) {
                    bands[i - 1] = new Band(bands[i - 1].Frequency, bands[i].Gain);
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
        /// Add windowing on the right of the curve. Windowing is applied logarithmically.
        /// </summary>
        public void Window(Window right, double startFreq, double endFreq) => Windowing.ApplyWindow(bands, right, startFreq, endFreq);

        /// <summary>
        /// Get which band is the first after a given <paramref name="freq"/>uency. Returns -1 if such a band was not found.
        /// </summary>
        int GetFirstBand(double freq) {
            for (int i = 0, c = bands.Count; i < c; i++) {
                if (bands[i].Frequency >= freq) {
                    return i;
                }
            }
            return -1;
        }
    }
}