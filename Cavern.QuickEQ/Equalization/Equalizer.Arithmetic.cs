using System;
using System.Collections.Generic;
using System.Linq;

namespace Cavern.QuickEQ.Equalization {
    // Math operations with Equalizers
    partial class Equalizer {
        /// <summary>
        /// Add the two <see cref="Equalizer"/>s together.
        /// </summary>
        /// <remarks>Matching frequencies have to be guaranteed before calling this function with <see cref="HasTheSameFrequenciesAs(Equalizer)"/>.
        /// For a safe version that allows different bands, use <see cref="Merge(Equalizer)"/>.
        /// For subtraction instead of addition, use <see cref="AlignTo(Equalizer)"/>.</remarks>
        public void AddCurve(Equalizer other) {
            List<Band> otherBands = other.bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                bands[i] += otherBands[i].Gain;
            }
            RecalculatePeakGain();
        }

        /// <summary>
        /// Apply a slope (in decibels, per octave) between two frequencies.
        /// </summary>
        public void AddSlope(double slope, double startFreq, double endFreq) {
            double logStart = Math.Log(startFreq),
                range = Math.Log(endFreq) - logStart;
            slope *= 1.44269504089f; // Slope /= log(2)
            double slopeMax = slope * range;
            for (int i = GetFirstBandSafe(startFreq), c = bands.Count; i < c; i++) {
                if (bands[i].Frequency > endFreq) {
                    bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slopeMax);
                } else {
                    bands[i] = new Band(bands[i].Frequency, bands[i].Gain + slope * (Math.Log(bands[i].Frequency) - logStart));
                }
            }
            RecalculatePeakGain();
        }

        /// <summary>
        /// Set this equalizer so if the <paramref name="other"/> is linear, this will be the difference from it.
        /// This is calculated by LHS (this instance) - RHS (<paramref name="other"/>) for each value.
        /// </summary>
        /// <remarks>Matching frequencies have to be guaranteed before calling this function with <see cref="HasTheSameFrequenciesAs(Equalizer)"/>.
        /// For addition instead of subtraction, use <see cref="AlignTo(Equalizer)"/>.</remarks>
        public void AlignTo(Equalizer other) {
            List<Band> otherBands = other.bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                bands[i] -= otherBands[i].Gain;
            }
            RecalculatePeakGain();
        }

        /// <summary>
        /// Get a curve showing the slope of the <see cref="Equalizer"/>.
        /// </summary>
        /// <remarks>The band count and band frequencies will change.</remarks>
        public Equalizer Derive() {
            if (bands.Count < 2) {
                return (Equalizer)Clone(); // No slope can be calculated
            }

            List<Band> newBands = new List<Band>(bands.Count - 1);
            for (int i = 0, c = bands.Count - 1; i < c; i++) {
                double freq = (bands[i].Frequency + bands[i + 1].Frequency) / 2;
                double slope = (bands[i + 1].Gain - bands[i].Gain) / (bands[i + 1].Frequency - bands[i].Frequency);
                newBands.Add(new Band(freq, slope));
            }
            return new Equalizer(newBands, true);
        }

        /// <summary>
        /// Merge this Equalizer with another, summing their gains.
        /// </summary>
        /// <remarks>For a faster version when both <see cref="Equalizer"/>s have the same bands, use
        /// <see cref="AddCurve(Equalizer)"/> for optimization.</remarks>
        public Equalizer Merge(Equalizer with) {
            List<Band> output = new List<Band>();
            for (int band = 0, bandc = bands.Count; band < bandc; band++) {
                output.Add(new Band(bands[band].Frequency, bands[band].Gain + with[bands[band].Frequency]));
            }
            for (int band = 0, bandc = with.bands.Count; band < bandc; band++) {
                output.Add(new Band(with.bands[band].Frequency, with.bands[band].Gain + this[with.bands[band].Frequency]));
            }
            output.Sort();
            return new Equalizer(output.Distinct().ToList(), true);
        }
    }
}
