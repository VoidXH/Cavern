using System;
using System.Collections.Generic;

using Cavern.QuickEQ.EQCurves;

namespace Cavern.QuickEQ.Equalization {
    // Implementation of Cavern's Valley Correction algorithm
    partial class Equalizer {
        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// The maximum gain increase is 6 dB, any band that needs more than this is considered a valley.
        /// </summary>
        public void ValleyCorrection(float[] measurement, EQCurve targetEQ, double startFreq, double stopFreq, float targetGain) =>
            ValleyCorrection(measurement, targetEQ, startFreq, stopFreq, targetGain, 6);

        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// The maximum gain increase is defined in the <paramref name="maxGain"/> parameter in decibels,
        /// any band that needs more than this is considered a valley.
        /// </summary>
        public void ValleyCorrection(float[] measurement, EQCurve targetEQ, double startFreq, double stopFreq, float targetGain, float maxGain) {
            int start = 0, end = measurement.Length - 1;
            float[] target = targetEQ.GenerateLogCurve(startFreq, stopFreq, measurement.Length, targetGain);
            while (start < end && target[start] > measurement[start] + maxGain) {
                start++; // find low extension
            }
            while (start < end && target[end] > measurement[end] + maxGain) {
                end--; // find high extension
            }
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(stopFreq) - startPow) / measurement.Length;
            for (int i = start; i <= end; i++) {
                if (target[i] >= measurement[i] + maxGain) {
                    start = i;
                    while (start != 0 && measurement[start] < target[start]) {
                        start--;
                    }
                    double firstFreq = Math.Pow(10, startPow + powRange * start);
                    while (i < end && measurement[i] < target[i]) {
                        i++;
                    }
                    double endFreq = Math.Pow(10, startPow + powRange * i) * 1.01;
                    for (int band = 0, bandc = bands.Count; band < bandc; band++) {
                        double bandFreq = bands[band].Frequency;
                        if (bandFreq < firstFreq) {
                            continue;
                        }
                        if (bandFreq > endFreq) {
                            break;
                        }
                        RemoveBand(bands[band--]);
                        bandc--;
                    }
                }
            }
        }

        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// The maximum gain increase is defined in the <paramref name="maxGain"/> parameter in decibels,
        /// any band that needs more than this is considered a valley.
        /// </summary>
        /// <param name="startFreq">First frequency to operate on</param>
        /// <param name="stopFreq">Last frequency to operate on</param>
        /// <param name="maxGain">Cut off bands that reach that much.</param>
        public void ValleyCorrection(double startFreq, double stopFreq, double maxGain) {
            (int start, int end) = GetMeasurementLimits(startFreq, stopFreq);
            for (int i = start; i <= end; i++) {
                if (bands[i].Gain >= maxGain) {
                    start = i;
                    while (start != 0 && bands[start].Gain > 0) {
                        start--;
                    }
                    while (i < end && bands[i].Gain > 0) {
                        i++;
                    }

                    int cut = i - start + 1;
                    bands.RemoveRange(start, cut);
                    i = start;
                    end -= cut;
                }
            }
        }

        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// The maximum gain increase is defined in the <paramref name="maxGain"/> parameter in decibels,
        /// any band that needs more than this is considered a valley.
        /// </summary>
        /// <param name="measurement">What was actually measured - must match elements with the correction (this),
        /// use <see cref="HasTheSameFrequenciesAs(Equalizer)"/> to check</param>
        /// <param name="targetEQ">Target curve to reach</param>
        /// <param name="startFreq">First frequency to operate on</param>
        /// <param name="stopFreq">Last frequency to operate on</param>
        /// <param name="targetGain">The EQ shall be ofset by this much (usually the average of the measured <paramref name="measurement"/>)</param>
        /// <param name="maxGain">Cut off bands that reach that much.</param>
        public void ValleyCorrection(Equalizer measurement, EQCurve targetEQ, double startFreq, double stopFreq, double targetGain, double maxGain) {
            (int start, int end) = GetMeasurementLimits(startFreq, stopFreq);
            double gainDiff = targetGain - maxGain; // Optimization to remove one addition from each check
            List<Band> measurementBands = measurement.bands;
            for (int i = end; i > 0; i--) {
                if (targetEQ[measurementBands[i].Frequency] > measurementBands[i].Gain + maxGain) {
                    int cutUntil = i;
                    while (i != 0 && measurementBands[i].Gain < targetEQ[measurementBands[i].Frequency]) {
                        i--;
                    }
                    while (cutUntil < end && measurementBands[cutUntil].Gain < targetEQ[measurementBands[cutUntil].Frequency]) {
                        cutUntil++;
                    }

                    int cut = cutUntil - i + 1;
                    bands.RemoveRange(start, cut);
                }
            }
        }

        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// The maximum gain increase is defined in the <paramref name="maxGain"/> parameter in decibels,
        /// any band that needs more than this is considered a valley.
        /// </summary>
        /// <param name="measurement">What was actually measured - must match elements with the correction (this),
        /// use <see cref="HasTheSameFrequenciesAs(Equalizer)"/> to check</param>
        /// <param name="targetCurve">Target curve to reach at the target gain - must match elements with the correction (this),
        /// use <see cref="HasTheSameFrequenciesAs(Equalizer)"/> to check</param>
        /// <param name="startFreq">First frequency to operate on</param>
        /// <param name="stopFreq">Last frequency to operate on</param>
        /// <param name="maxGain">Cut off bands that reach that much.</param>
        public void ValleyCorrection(Equalizer measurement, Equalizer targetCurve, double startFreq, double stopFreq, double maxGain) {
            (int start, int end) = GetMeasurementLimits(startFreq, stopFreq);
            List<Band> measurementBands = measurement.bands;
            List<Band> targetBands = targetCurve.bands;
            for (int i = end; i > 0; i--) {
                if (targetBands[i].Gain > measurementBands[i].Gain + maxGain) {
                    int cutUntil = i;
                    while (i != 0 && measurementBands[i].Gain < targetBands[i].Gain) {
                        i--;
                    }
                    while (cutUntil < end && measurementBands[cutUntil].Gain < targetBands[cutUntil].Gain) {
                        cutUntil++;
                    }

                    int cut = cutUntil - i + 1;
                    bands.RemoveRange(start, cut);
                }
            }
        }
    }
}
