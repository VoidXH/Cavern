using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.QuickEQ.EQCurves;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Equalizer data collector and exporter.
    /// </summary>
    public sealed partial class Equalizer : ICloneable {
        /// <summary>
        /// Bands that make up this equalizer.
        /// </summary>
        public IReadOnlyList<Band> Bands => bands;
        readonly List<Band> bands;

        /// <summary>
        /// Gets the gain at a given frequency.
        /// </summary>
        public double this[double frequency] {
            get {
                int bandCount = bands.Count;
                if (bandCount == 0) {
                    return 0;
                }
                int nextBand = 0, prevBand = 0;
                while (nextBand != bandCount && bands[nextBand].Frequency < frequency) {
                    prevBand = nextBand;
                    nextBand++;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    double logFrom = Math.Log(bands[prevBand].Frequency),
                        logTo = Math.Log(bands[nextBand].Frequency);
                    return QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain, QMath.LerpInverse(logFrom, logTo, Math.Log(frequency)));
                }
                return bands[prevBand].Gain;
            }
        }

        /// <summary>
        /// Cut off low frequencies that are out of the channel's frequency range.
        /// </summary>
        public bool SubsonicFilter {
            get => subsonicFilter;
            set {
                if (subsonicFilter && !value) {
                    if (bands.Count > 0) {
                        bands.RemoveAt(0);
                    }
                } else if (!subsonicFilter && value && bands.Count > 0) {
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                }
                subsonicFilter = value;
            }
        }
        bool subsonicFilter;

        /// <summary>
        /// Subsonic filter rolloff in dB / octave.
        /// </summary>
        public double SubsonicRolloff {
            get => subsonicRolloff;
            set => Modify(() => subsonicRolloff = value);
        }
        double subsonicRolloff = 24;

        /// <summary>
        /// The highest gain in this EQ.
        /// </summary>
        public double PeakGain { get; private set; }

        /// <summary>
        /// Frequency of the leftmost band.
        /// </summary>
        public double StartFrequency => bands[0].Frequency;

        /// <summary>
        /// Frequency of the rightmost band.
        /// </summary>
        public double EndFrequency => bands[^1].Frequency;

        /// <summary>
        /// Equalizer data collector and exporter.
        /// </summary>
        public Equalizer() => bands = new List<Band>();

        /// <summary>
        /// Equalizer data collector and exporter from a previously created set of bands.
        /// </summary>
        /// <param name="bands">Bands to add</param>
        /// <param name="sorted">The bands are in ascending order by frequency</param>
        public Equalizer(List<Band> bands, bool sorted) {
            if (!sorted) {
                bands.Sort();
            }
            this.bands = bands;
            RecalculatePeakGain();
        }

        /// <summary>
        /// Equalizer data collector and exporter from a binary stream created with <see cref="BinarySerialize(BinaryWriter, bool)"/>.
        /// </summary>
        public Equalizer(BinaryReader reader) : this() => BinaryDeserialize(reader);

        /// <summary>
        /// Create a copy of this EQ with the same bands.
        /// </summary>
        public object Clone() {
            Equalizer result = new Equalizer(bands.ToList(), true) {
                subsonicFilter = subsonicFilter,
                subsonicRolloff= subsonicRolloff,
            };
            return result;
        }

        /// <summary>
        /// Add a new band to the EQ.
        /// </summary>
        public void AddBand(Band newBand) => Modify(() => {
            if (bands.Count == 0 || PeakGain < newBand.Gain) {
                PeakGain = newBand.Gain;
            }
            bands.AddSortedDistinct(newBand);
        });

        /// <summary>
        /// Remove a band from the EQ.
        /// </summary>
        public void RemoveBand(Band removable) => Modify(() => {
            bands.RemoveSorted(removable);
            if (bands.Count == 0) {
                PeakGain = 0;
            } else if (PeakGain == removable.Gain) {
                RecalculatePeakGain();
            }
        });

        /// <summary>
        /// Remove multiple bands from the EQ.
        /// </summary>
        /// <param name="first">First band</param>
        /// <param name="count">Number of bands to remove starting with <paramref name="first"/></param>
        public void RemoveBands(Band first, int count) => Modify(() => {
            int start = bands.BinarySearch(first);
            if (start >= 0) {
                bool recalculatePeak = false;
                for (int i = 0; i < count; i++) {
                    if (bands[start + i].Gain == PeakGain) {
                        recalculatePeak = true;
                        break;
                    }
                }
                bands.RemoveRange(start, count);
                if (bands.Count == 0) {
                    PeakGain = 0;
                } else if (recalculatePeak) {
                    RecalculatePeakGain();
                }
            }
        });

        /// <summary>
        /// Reset this EQ.
        /// </summary>
        public void ClearBands() => Modify(() => {
            PeakGain = 0;
            bands.Clear();
        });

        /// <summary>
        /// Add gain in decibels to all bands.
        /// </summary>
        public void Offset(double gain) {
            for (int i = 0, c = bands.Count; i < c; i++) {
                bands[i] += gain;
            }
            PeakGain += gain;
        }

        /// <summary>
        /// Compares the two EQs if they have values at the same frequencies.
        /// </summary>
        public bool HasTheSameFrequenciesAs(Equalizer other) => HasTheSameFrequenciesAs(other, .0000001);

        /// <summary>
        /// Compares the two EQs if they have values at the same frequencies.
        /// </summary>
        public bool HasTheSameFrequenciesAs(Equalizer other, double maxError) {
            List<Band> otherBands = other.bands;
            int bandCount = bands.Count;
            if (bandCount != otherBands.Count) {
                return false;
            }
            for (int i = 0; i < bandCount; i++) {
                if (Math.Abs(bands[i].Frequency - otherBands[i].Frequency) > maxError) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Flip the <see cref="Equalizer"/> vertically.
        /// </summary>
        public void Invert() {
            for (int i = 0, c = bands.Count; i < c; i++) {
                bands[i] = new Band(bands[i].Frequency, -bands[i].Gain);
            }
            RecalculatePeakGain();
        }

        /// <summary>
        /// Calibrate this Equalizer with another: keep the frequencies and subtract the recording device's added gains.
        /// </summary>
        public void Calibrate(Equalizer with) {
            for (int band = 0, bandc = bands.Count; band < bandc; band++) {
                bands[band] = new Band(bands[band].Frequency, bands[band].Gain - with[bands[band].Frequency]);
            }
        }

        /// <inheritdoc/>
        public override string ToString() {
            if (bands.Count == 0) {
                return "Empty equalizer";
            }
            return $"Equalizer with {bands.Count} bands ({StartFrequency:0}-{EndFrequency:0} Hz), peak: {PeakGain:0.00} dB";
        }

        /// <summary>
        /// Frame modifications to not break subsonic filtering.
        /// </summary>
        void Modify(Action action) {
            bool wasFiltered = subsonicFilter;
            subsonicFilter = false;
            if (wasFiltered && bands.Count > 0) {
                bands.RemoveAt(0);
            }
            action();
            if (wasFiltered) {
                if (bands.Count > 0) {
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                }
                subsonicFilter = true;
            }
        }

        /// <summary>
        /// Determine the highest amplification of the filter.
        /// </summary>
        void RecalculatePeakGain() {
            if (bands.Count == 0) {
                PeakGain = 0;
                return;
            }
            PeakGain = bands[0].Gain;
            for (int band = 1, count = bands.Count; band < count; band++) {
                if (PeakGain < bands[band].Gain) {
                    PeakGain = bands[band].Gain;
                }
            }
        }
    }
}