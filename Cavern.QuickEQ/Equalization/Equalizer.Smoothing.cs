using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.QuickEQ.Equalization.Enums;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Apply smoothing on this <see cref="Equalizer"/> with a window of a given octave.
        /// </summary>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <remarks>Smoothing happens in linear space, not with the raw decibel values.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Smooth(double octaves) => Smooth(octaves, SmoothingMode.GainSpace);

        /// <summary>
        /// Apply smoothing on this <see cref="Equalizer"/> with a window of a given octave.
        /// </summary>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <param name="mode">The space in which smoothing is applied.</param>
        public void Smooth(double octaves, SmoothingMode mode) {
            int count = bands.Count;
            if (count == 0) {
                return;
            }

            double multipleTo = Math.Pow(2, octaves);
            double multipleFrom = 1 / multipleTo;

            double[] gains = new double[count];
            switch (mode) {
                case SmoothingMode.DecibelSpace: {
                    for (int i = 0; i < count; i++) {
                        gains[i] = bands[i].Gain;
                    }
                    break;
                }
                case SmoothingMode.GainSpace: {
                    for (int i = 0; i < count; i++) {
                        gains[i] = QMath.DbToGain(bands[i].Gain);
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException("Equalizers don't hold complex values.");
            }

            double[] prefix = gains.PrefixSum();
            double[] result = new double[count];
            int smoothFrom = 0;
            int smoothTo = 0;
            for (int i = 0; i < count; i++) {
                double minFreq = bands[i].Frequency * multipleFrom;
                double maxFreq = bands[i].Frequency * multipleTo;

                while (smoothTo < count && bands[smoothTo].Frequency < maxFreq) {
                    smoothTo++;
                }

                while (smoothFrom < count && bands[smoothFrom].Frequency < minFreq) {
                    smoothFrom++;
                }

                int windowSize = smoothTo - smoothFrom;
                if (windowSize > 0) {
                    result[i] = (prefix[smoothTo] - prefix[smoothFrom]) / windowSize;
                } else {
                    result[i] = gains[i];
                }
            }

            if (mode == SmoothingMode.DecibelSpace) {
                for (int i = 0; i < count; i++) {
                    bands[i] = new Band(bands[i].Frequency, result[i]);
                }
            } else {
                for (int i = 0; i < count; i++) {
                    bands[i] = new Band(bands[i].Frequency, QMath.GainToDb(result[i]));
                }
            }
        }

        /// <summary>
        /// Apply a smoothing on this <see cref="Equalizer"/> that changes by frequency between two limits.
        /// </summary>
        /// <param name="startOctave">Smoothing window size in octaves at the beginning of the graph.</param>
        /// <param name="endOctave">Smoothing window size in octaves at the end of the graph.</param>
        /// <remarks>Smoothing happens in linear space, not with the raw decibel values.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Smooth(double startOctave, double endOctave) => Smooth(startOctave, endOctave, SmoothingMode.GainSpace);

        /// <summary>
        /// Apply a smoothing on this <see cref="Equalizer"/> that changes by frequency between two limits.
        /// </summary>
        /// <param name="startOctave">Smoothing window size in octaves at the beginning of the graph.</param>
        /// <param name="endOctave">Smoothing window size in octaves at the end of the graph.</param>
        /// <param name="mode">The space in which smoothing is applied.</param>
        public void Smooth(double startOctave, double endOctave, SmoothingMode mode)
            => Smooth(bands[0].Frequency, bands[^1].Frequency, startOctave, endOctave, mode);

        /// <summary>
        /// Apply a smoothing on this <see cref="Equalizer"/> that changes by frequency between two limits.
        /// </summary>
        /// <param name="startFreq">Start frequency of the interpolation range.</param>
        /// <param name="endFreq">End frequency of the interpolation range.</param>
        /// <param name="startOctave">Smoothing window size in octaves at <paramref name="startFreq"/>.</param>
        /// <param name="endOctave">Smoothing window size in octaves at <paramref name="endFreq"/>.</param>
        /// <remarks>Smoothing happens in linear space, not with the raw decibel values.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Smooth(double startFreq, double endFreq, double startOctave, double endOctave)
            => Smooth(startFreq, endFreq, startOctave, endOctave, SmoothingMode.GainSpace);

        /// <summary>
        /// Apply a smoothing on this <see cref="Equalizer"/> that changes by frequency between two limits.
        /// </summary>
        /// <param name="startFreq">Start frequency of the interpolation range.</param>
        /// <param name="endFreq">End frequency of the interpolation range.</param>
        /// <param name="startOctave">Smoothing window size in octaves at <paramref name="startFreq"/>.</param>
        /// <param name="endOctave">Smoothing window size in octaves at <paramref name="endFreq"/>.</param>
        /// <param name="mode">The space in which smoothing is applied.</param>
        public void Smooth(double startFreq, double endFreq, double startOctave, double endOctave, SmoothingMode mode) {
            int count = bands.Count;
            if (count == 0) {
                return;
            }

            Equalizer end = (Equalizer)Clone();
            Smooth(startOctave, mode);
            end.Smooth(endOctave, mode);
            List<Band> endBands = end.bands;

            double effectiveStartFreq = startFreq > 0 ? startFreq : bands[0].Frequency;
            for (int i = 0; i < count; i++) {
                double freq = bands[i].Frequency;
                double position = Math.Clamp(QMath.LorpInverse(effectiveStartFreq, endFreq, freq), 0.0, 1.0);
                bands[i] = new Band(freq, QMath.Lerp(bands[i].Gain, endBands[i].Gain, position));
            }
        }
    }
}
