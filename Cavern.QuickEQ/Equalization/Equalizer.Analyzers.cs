using System;
using System.Collections.Generic;
using System.Linq;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer : IEquatable<Equalizer> {
        /// <inheritdoc/>
        public bool Equals(Equalizer other) {
            int c = bands.Count;
            List<Band> others = other.bands;
            if (c != others.Count) {
                return false;
            }

            for (int i = 0; i < c; i++) {
                if (!bands[i].Equals(others[i])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get the average level between the <paramref name="minFreq"/> and <paramref name="maxFreq"/>,
        /// calculated in voltage scale, returned in dB scale.
        /// </summary>
        public double GetAverageLevel(double minFreq, double maxFreq) {
			int i = 0, c = bands.Count;
			while (i < c && bands[i].Frequency < minFreq) {
				i++;
			}
			double sum = 0;
			int n = 0;
			while (i < c && bands[i].Frequency <= maxFreq) {
				sum += Math.Pow(10, bands[i++].Gain * .05);
				n++;
			}
			return 20 * Math.Log10(sum / n);
		}

		/// <summary>
		/// Get the average level between the rolloff points (-<paramref name="range"/> dB points)
		/// of the curve drawn by this <see cref="Equalizer"/>. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
		/// the limits of a frequency band that can't be overly distorted on the curve and shall work for regression line calculation.
		/// </summary>
		public double GetAverageLevel(double range, double stableRangeStart, double stableRangeEnd) {
			(double minFreq, double maxFreq) = GetRolloffs(range, stableRangeStart, stableRangeEnd);
			return GetAverageLevel(minFreq, maxFreq);
		}

        /// <summary>
        /// Get the maximum difference between the gains of two <see cref="Equalizer"/>s at the same band.
        /// </summary>
        /// <remarks>Matching frequencies have to be guaranteed before calling this function with
        /// <see cref="HasTheSameFrequenciesAs(Equalizer)"/>.</remarks>
        public double GetMaximumDifference(Equalizer from) {
            List<Band> otherBands = from.bands;
			double max = Math.Abs(bands[0].Gain - otherBands[0].Gain);
            for (int i = 1, c = bands.Count; i < c; i++) {
				double current = Math.Abs(bands[i].Gain - otherBands[i].Gain);
				if (max < current) {
					max = current;
				}
			}
			return max;
		}

        /// <summary>
        /// Get the maximum difference between the gains of two <see cref="Equalizer"/>s at the same band.
        /// </summary>
        /// <param name="from">Compare with this <see cref="Equalizer"/></param>
        /// <param name="startFreq">First frequency to consider when calculating the maximum difference</param>
		/// <param name="endFreq">Last frequency to consider when calculating the maximum difference</param>
        /// <remarks>Matching frequencies have to be guaranteed before calling this function with
        /// <see cref="HasTheSameFrequenciesAs(Equalizer)"/>.</remarks>
        public double GetMaximumDifference(Equalizer from, double startFreq, double endFreq) {
            List<Band> otherBands = from.bands;
            (int first, int last) = GetBandLimits(startFreq, endFreq);
            double max = 0;
            for (int i = first, c = last; i < c; i++) {
                double current = Math.Abs(bands[i].Gain - otherBands[i].Gain);
                if (max < current) {
                    max = current;
                }
            }
            return max;
        }

        /// <summary>
        /// Get the median level between the <paramref name="minFreq"/> and <paramref name="maxFreq"/>.
        /// </summary>
        public double GetMedianLevel(double minFreq, double maxFreq) {
			int i = 0, c = bands.Count;
			while (i < c && bands[i].Frequency < minFreq) {
				i++;
			}
			int startBand = i;
			while (i < c && bands[i].Frequency <= maxFreq) {
				i++;
			}

            List<double> sortedBands = bands.GetRange(startBand, i - startBand).Select(x => x.Gain).ToList();
			sortedBands.Sort();
			return sortedBands[sortedBands.Count >> 1];
		}

        /// <summary>
        /// Calculate average band deviation with a given <paramref name="bandwidth"/> for each non-overlapping band between
        /// <paramref name="startFreq"/> and <paramref name="endFreq"/>.
        /// </summary>
        public double GetNarrowBandDeviation(double startFreq, double endFreq, double bandwidth) {
            double bandFreqMul = Math.Pow(2, bandwidth); // Multiply the start of a band with this to get the end
            double sum = 0;
            int measuredBands = 0;
            for (int i = GetFirstBandSafe(startFreq), c = bands.Count; i < c && bands[i].Frequency <= endFreq; i++) {
                double bandEnd = bands[i].Frequency * bandFreqMul;
                double avg = GetAverageLevel(startFreq, bandEnd);
                for (; i < c && bands[i].Frequency <= bandEnd; i++) {
                    // Go to the next band
                }
                sum += Math.Abs(avg - this[startFreq * Math.Sqrt(bandFreqMul)]); // Average amplitude - middle of the band amplitude
                measuredBands++;
            }
            return sum / measuredBands;
        }

        /// <summary>
        /// Get a line in the form of slope * log10(x) + intercept that fits the curve drawn by this <see cref="Equalizer"/>.
        /// The regression line will only be calculated on the bands between <paramref name="startFreq"/> and <paramref name="endFreq"/>.
        /// </summary>
        /// <param name="startFreq">First frequency to consider when calculating the regression line</param>
        /// <param name="endFreq">Last frequency to consider when calculating the regression line</param>
        public (double slope, double intercept) GetRegressionLine(double startFreq, double endFreq) {
			double freqSum = 0,
				freqSqSum = 0,
				gainSum = 0,
				mac = 0,
				n = 0;
			for (int i = GetFirstBandSafe(startFreq), c = bands.Count; i < c && bands[i].Frequency <= endFreq; i++) {
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
        /// <param name="range">Rolloff points are the furthest points from the stable range that are
        /// this far away from the regression line in decibels</param>
        /// <param name="stableRangeStart">First frequency to consider when calculating the regression line</param>
        /// <param name="stableRangeEnd">Last frequency to consider when calculating the regression line</param>
        public (double minFreq, double maxFreq) GetRolloffs(double range, double stableRangeStart, double stableRangeEnd) =>
			GetRolloffs(range, stableRangeStart, stableRangeEnd, double.NaN, double.NaN);

        /// <summary>
        /// Get the rolloff points of the EQ by fitting a line on it and finding the first and last points
        /// with a given range in decibels below it. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
        /// the limits of a frequency band that can't be overly distorted on the curve and shall work for regression line calculation.
        /// </summary>
		/// <param name="range">Rolloff points are the furthest points from the stable range that are
		/// this far away from the regression line in decibels</param>
		/// <param name="stableRangeStart">First frequency to consider when calculating the regression line</param>
		/// <param name="stableRangeEnd">Last frequency to consider when calculating the regression line</param>
		/// <param name="measurementRangeStart">First frequency to check for regression line distance -
		/// used for mitigating low-frequency anomalies</param>
		/// <param name="measurementRangeEnd">Last frequency to check for regression line distance -
		/// useful for limiting the calculation range for performance when you only need the lower rolloff</param>
        public (double minFreq, double maxFreq) GetRolloffs(double range, double stableRangeStart, double stableRangeEnd,
			double measurementRangeStart, double measurementRangeEnd) {
            (double m, double b) = GetRegressionLine(stableRangeStart, stableRangeEnd);
            (int first, int last) = GetMeasurementLimits(measurementRangeStart, measurementRangeEnd);
            while (first < last && bands[first].Gain + range < m * Math.Log10(bands[first].Frequency) + b) {
                first++;
            }
            while (last > first && bands[last].Gain + range < m * Math.Log10(bands[last].Frequency) + b) {
                last--;
            }
            return (bands[first].Frequency, bands[last].Frequency);
        }

        /// <summary>
        /// Get the rolloff points of the EQ by calculating its mean and finding the first and last points
        /// with a given range in decibels below it. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
        /// the limits of a frequency band that can't be overly distorted on the curve and shall work for mean calculation.
        /// </summary>
		/// <param name="range">Rolloff points are the furthest points from the stable range that are
		/// this far away from the mean in decibels</param>
		/// <param name="stableRangeStart">First frequency to consider when calculating the mean</param>
		/// <param name="stableRangeEnd">Last frequency to consider when calculating the mean</param>
        public (double minFreq, double maxFreq) GetRolloffsNaive(double range, double stableRangeStart, double stableRangeEnd) =>
            GetRolloffsNaive(range, stableRangeStart, stableRangeEnd, double.NaN, double.NaN);

        /// <summary>
        /// Get the rolloff points of the EQ by calculating its mean and finding the first and last points
        /// with a given range in decibels below it. <paramref name="stableRangeStart"/> and <paramref name="stableRangeEnd"/> are
        /// the limits of a frequency band that can't be overly distorted on the curve and shall work for mean calculation.
        /// </summary>
		/// <param name="range">Rolloff points are the furthest points from the stable range that are
		/// this far away from the mean in decibels</param>
		/// <param name="stableRangeStart">First frequency to consider when calculating the mean</param>
		/// <param name="stableRangeEnd">Last frequency to consider when calculating the mean</param>
		/// <param name="measurementRangeStart">First frequency to check for mean distance -
		/// used for mitigating low-frequency anomalies</param>
		/// <param name="measurementRangeEnd">Last frequency to check for mean distance -
		/// useful for limiting the calculation range for performance when you only need the lower rolloff</param>
        public (double minFreq, double maxFreq) GetRolloffsNaive(double range, double stableRangeStart, double stableRangeEnd,
            double measurementRangeStart, double measurementRangeEnd) {
            (int first, int last) = GetMeasurementLimits(measurementRangeStart, measurementRangeEnd);
            double mean = GetAverageLevel(stableRangeStart, stableRangeEnd);
            while (first < last && bands[first].Gain + range < mean) {
                first++;
            }
            while (last > first && bands[last].Gain + range < mean) {
                last--;
            }
            return (bands[first].Frequency, bands[last].Frequency);
        }

        /// <summary>
        /// Get the smoothness of a linear regression line between <paramref name="startFreq"/> and <paramref name="endFreq"/>,
        /// also known as R^2.
        /// </summary>
        /// <param name="startFreq">First frequency to consider</param>
        /// <param name="endFreq">Last frequency to consider</param>
        public double GetSmoothness(double startFreq, double endFreq) {
            double freqSum = 0,
                freqSqSum = 0,
                gainSum = 0,
                gainSqSum = 0,
                mac = 0,
                n = 0;
            for (int i = GetFirstBandSafe(startFreq), c = bands.Count; i < c && bands[i].Frequency <= endFreq; i++) {
                double freq = bands[i].Frequency;
                freqSum += freq;
                freqSqSum += freq * freq;
                double gain = bands[i].Gain;
                gainSum += gain;
                gainSqSum += gain * gain;
                mac += freq * gain;
                n++;
            }
            double r = (n * mac - freqSum * gainSum) / Math.Sqrt((n * freqSqSum - freqSum * freqSum) * (n * gainSqSum - gainSum * gainSum));
            return r * r;
        }

        /// <summary>
        /// Detects valleys as band index ranges in the spectrum by comparing this <see cref="Equalizer"/> with an oversmoothed version.
        /// </summary>
        /// <param name="depth">Minimum valley in decibels for a range to be considered a result</param>
        /// <param name="oversmoothing">Compare the current <see cref="Equalizer"/>
        /// to a version which is smoothed by this many octaves</param>
        public List<(int startInclusive, int stopExclusive)> GetValleys(double depth, double oversmoothing) {
            Equalizer smoothed = (Equalizer)Clone();
            smoothed.Smooth(oversmoothing);
            List<(int, int)> result = new List<(int, int)>();
            IReadOnlyList<Band> dryBands = bands,
                wetBands = smoothed.bands;
            bool inValley = false;
            int valleyStarted = 0;
            double maxInValley = 0;
            for (int i = 0, c = dryBands.Count; i < c; i++) {
                double valley = wetBands[i].Gain - dryBands[i].Gain;
                bool willBeInValley = valley > 0;
                if (maxInValley < valley) {
                    maxInValley = valley;
                }
                if (inValley != willBeInValley) {
                    if (willBeInValley) {
                        maxInValley = valley;
                        valleyStarted = i;
                    } else if (inValley && maxInValley >= depth) {
                        result.Add((valleyStarted, i));
                    }
                    inValley = willBeInValley;
                }
            }
            return result;
        }

        /// <summary>
        /// Get the average level between the <paramref name="firstBand"/> (inclusive) and <paramref name="lastBand"/> (exclusive),
        /// calculated in voltage scale, returned in dB scale.
        /// </summary>
        double GetAverageLevel(int firstBand, int lastBand) {
			double sum = 0;
			int n = 0;
			while (firstBand < lastBand) {
				sum += Math.Pow(10, bands[firstBand++].Gain * .05);
				n++;
			}
			return 20 * Math.Log10(sum / n);
		}
	}
}