using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.QuickEQ.EQCurves;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>Equalizer data collector and exporter.</summary>
    public sealed class Equalizer {
        /// <summary>Bands that make up this equalizer.</summary>
        public IReadOnlyList<Band> Bands => bands;
        readonly List<Band> bands = new List<Band>();

        /// <summary>Equalizer data collector and exporter.</summary>
        public Equalizer() { }

        /// <summary>Equalizer data collector and exporter from a previously created set of bands.</summary>
        /// <remarks>The list of bands must be sorted.</remarks>
        internal Equalizer(List<Band> bands) {
            this.bands = bands;
            RecalculatePeakGain();
        }

        /// <summary>Gets the gain at a given frequency.</summary>
        public double this[double frequency] {
            get {
                int bandCount = bands.Count;
                if (bandCount == 0)
                    return 0;
                int nextBand = 0, prevBand = 0;
                while (nextBand != bandCount && bands[nextBand].Frequency < frequency) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    return QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, frequency));
                return bands[prevBand].Gain;
            }
        }

        /// <summary>Cut off low frequencies that are out of the channel's frequency range.</summary>
        public bool SubsonicFilter {
            get => subsonicFilter;
            set {
                if (subsonicFilter && !value) {
                    if (bands.Count > 0)
                        bands.RemoveAt(0);
                } else if (!subsonicFilter && value && bands.Count > 0)
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                subsonicFilter = value;
            }
        }
        bool subsonicFilter = false;

        /// <summary>Frame modifications to not break subsonic filtering.</summary>
        void Modify(Action action) {
            bool wasFiltered = subsonicFilter;
            subsonicFilter = false;
            if (wasFiltered && bands.Count > 0)
                bands.RemoveAt(0);
            action();
            if (wasFiltered) {
                if (bands.Count > 0)
                    AddBand(new Band(bands[0].Frequency * .5f, bands[0].Gain - subsonicRolloff));
                subsonicFilter = true;
            }
        }

        /// <summary>Subsonic filter rolloff in dB / octave.</summary>
        public double SubsonicRolloff {
            get => subsonicRolloff;
            set => Modify(() => subsonicRolloff = value);
        }
        double subsonicRolloff = 24;

        /// <summary>The highest gain in this EQ.</summary>
        public double PeakGain { get; private set; }

        void RecalculatePeakGain() {
            if (bands.Count == 0) {
                PeakGain = 0;
                return;
            }
            PeakGain = bands[0].Gain;
            for (int band = 1, count = bands.Count; band < count; ++band)
                if (PeakGain < bands[band].Gain)
                    PeakGain = bands[band].Gain;
        }

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band newBand) => Modify(() => {
            if (bands.Count == 0 || PeakGain < newBand.Gain)
                PeakGain = newBand.Gain;
            bands.AddSortedDistinct(newBand);
        });

        /// <summary>Remove a band from the EQ.</summary>
        public void RemoveBand(Band removable) => Modify(() => {
            bands.RemoveSorted(removable);
            if (bands.Count == 0)
                PeakGain = 0;
            else if (PeakGain == removable.Gain)
                RecalculatePeakGain();
        });

        /// <summary>Remove multiple bands from the EQ.</summary>
        /// <param name="first">First band</param>
        /// <param name="count">Number of bands to remove starting with <paramref name="first"/></param>
        public void RemoveBands(Band first, int count) => Modify(() => {
            int start = bands.BinarySearch(first);
            if (start >= 0) {
                bool recalculatePeak = false;
                for (int i = 0; i < count; ++i) {
                    if (bands[start + i].Gain == PeakGain) {
                        recalculatePeak = true;
                        break;
                    }
                }
                bands.RemoveRange(start, count);
                if (bands.Count == 0)
                    PeakGain = 0;
                else if (recalculatePeak)
                    RecalculatePeakGain();
            }
        });

        /// <summary>Reset this EQ.</summary>
        public void ClearBands() => Modify(() => {
            PeakGain = 0;
            bands.Clear();
        });

        /// <summary>Shows the EQ curve in a linearly scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] VisualizeLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0)
                return result;
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0, nextBand = 0, prevBand = 0; entry < length; ++entry) {
                double freq = startFreq + step * entry;
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    result[entry] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                else
                    result[entry] = (float)bands[prevBand].Gain;
            }
            return result;
        }

        /// <summary>Gets the corresponding frequencies for <see cref="VisualizeLinear(double, double, int)"/>.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public static float[] FrequenciesLinear(double startFreq, double endFreq, int length) =>
            SweepGenerator.LinearFreqs(startFreq, endFreq, length);

        /// <summary>Shows the EQ curve in a logarithmically scaled frequency axis.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0)
                return result;
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0, nextBand = 0, prevBand = 0; i < length; ++i) {
                while (nextBand != bandCount && bands[nextBand].Frequency < startFreq) {
                    prevBand = nextBand;
                    ++nextBand;
                }
                if (nextBand != bandCount && nextBand != 0)
                    result[i] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, startFreq));
                else
                    result[i] = (float)bands[prevBand].Gain;
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>Gets the corresponding frequencies for <see cref="Visualize(double, double, int)"/>.</summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public static float[] Frequencies(double startFreq, double endFreq, int length) =>
            SweepGenerator.ExponentialFreqs(startFreq, endFreq, length);

        /// <summary>Shows the resulting frequency response if this EQ is applied.</summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, double startFreq, double endFreq) {
            float[] filter = Visualize(startFreq, endFreq, response.Length);
            for (int i = 0; i < response.Length; ++i)
                filter[i] += response[i];
            return filter;
        }

        /// <summary>Apply this EQ on a frequency response.</summary>
        /// <param name="response">Frequency response to apply the EQ on</param>
        /// <param name="sampleRate">Sample rate where <paramref name="response"/> was generated</param>
        public void Apply(Complex[] response, int sampleRate) {
            int halfLength = response.Length / 2 + 1, nyquist = sampleRate / 2;
            float[] filter = VisualizeLinear(0, nyquist, halfLength);
            response[0] *= (float)Math.Pow(10, filter[0] * .05f);
            for (int i = 1, end = response.Length; i < halfLength; ++i) {
                response[i] *= (float)Math.Pow(10, filter[i] * .05f);
                response[end - i] = new Complex(response[i].Real, -response[i].Imaginary);
            }
        }

        /// <summary>Merge this Equalizer with another, summing their gains.</summary>
        public Equalizer Merge(Equalizer with) {
            List<Band> output = new List<Band>();
            for (int band = 0, bandc = bands.Count; band < bandc; ++ band)
                output.Add(new Band(bands[band].Frequency, bands[band].Gain + with[bands[band].Frequency]));
            for (int band = 0, bandc = with.bands.Count; band < bandc; ++band)
                output.Add(new Band(with.bands[band].Frequency, with.bands[band].Gain + this[with.bands[band].Frequency]));
            output.Sort();
            return new Equalizer(output.Distinct().ToList());
        }

        /// <summary>
        /// Remove correction from spectrum vallies that are most likely measurement errors or uncorrectable room modes.
        /// </summary>
        public void ValleyCorrection(float[] curve, EQCurve targetEQ, double startFreq, double stopFreq, float targetGain, float maxGain = 6) {
            int start = 0, end = curve.Length - 1;
            float[] target = targetEQ.GenerateLogCurve(curve.Length, startFreq, stopFreq, targetGain);
            while (start < end && target[start] > curve[start] + maxGain)
                ++start; // find low extension
            while (start < end && target[end] > curve[end] + maxGain)
                --end; // find high extension
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(stopFreq) - startPow) / curve.Length;
            for (int i = start; i <= end; ++i) {
                if (target[i] > curve[i] + maxGain) {
                    start = i;
                    while (start != 0 && curve[start] < target[start])
                        --start;
                    double firstFreq = Math.Pow(10, startPow + powRange * start);
                    while (i < end && curve[i] < target[i])
                        ++i;
                    double endFreq = Math.Pow(10, startPow + powRange * i) * 1.01;
                    for (int band = 0, bandc = bands.Count; band < bandc; ++band) {
                        double bandFreq = bands[band].Frequency;
                        if (bandFreq < firstFreq)
                            continue;
                        if (bandFreq > endFreq)
                            break;
                        RemoveBand(bands[band--]);
                        --bandc;
                    }
                }
            }
        }
    }
}