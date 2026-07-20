using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Apply smoothing on this <see cref="Equalizer"/> with a window of a given octave.
        /// </summary>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <remarks>Smoothing happens in linear space, not with the raw decibel values.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Smooth(double octaves) => Smooth(octaves, false);

        /// <summary>
        /// Apply smoothing on this <see cref="Equalizer"/> with a window of a given octave.
        /// </summary>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <param name="decibelSpace">Normally this function smooths the gain curve, this option disables converting back to it for processing.</param>
        public void Smooth(double octaves, bool decibelSpace) {
            int count = bands.Count;
            if (count == 0) {
                return;
            }

            double multipleTo = Math.Pow(2, octaves);
            double multipleFrom = 1 / multipleTo;

            double[] gains = new double[count];
            if (decibelSpace) {
                for (int i = 0; i < count; i++) {
                    gains[i] = bands[i].Gain;
                }
            } else {
                for (int i = 0; i < count; i++) {
                    gains[i] = QMath.DbToGain(bands[i].Gain);
                }
            }

            double[] result = new double[count];
            int smoothFrom = 0;
            int smoothTo = 0;
            double currentWindowSum = 0;
            for (int i = 0; i < count; i++) {
                double minFreq = bands[i].Frequency * multipleFrom;
                double maxFreq = bands[i].Frequency * multipleTo;

                while (smoothTo < count && bands[smoothTo].Frequency < maxFreq) {
                    currentWindowSum += gains[smoothTo];
                    smoothTo++;
                }

                while (smoothFrom < count && bands[smoothFrom].Frequency < minFreq) {
                    currentWindowSum -= gains[smoothFrom];
                    smoothFrom++;
                }

                int windowSize = smoothTo - smoothFrom;
                if (windowSize > 0) {
                    result[i] = currentWindowSum / windowSize;
                } else {
                    result[i] = gains[i];
                }
            }

            if (decibelSpace) {
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
        public void Smooth(double startOctave, double endOctave) => Smooth(startOctave, endOctave, false);

        /// <summary>
        /// Apply a smoothing on this <see cref="Equalizer"/> that changes by frequency between two limits.
        /// </summary>
        /// <param name="startOctave">Smoothing window size in octaves at the beginning of the graph.</param>
        /// <param name="endOctave">Smoothing window size in octaves at the end of the graph.</param>
        /// <param name="decibelSpace">Normally this function smooths the gain curve, this option disables converting back to it for processing.</param>
        public void Smooth(double startOctave, double endOctave, bool decibelSpace) {
            Equalizer end = (Equalizer)Clone();
            Smooth(startOctave, decibelSpace);
            end.Smooth(endOctave, decibelSpace);
            List<Band> endBands = end.bands;
            int count = bands.Count;
            float positioner = 1f / count;
            for (int i = 0; i < count; i++) {
                bands[i] = new Band(bands[i].Frequency, QMath.Lerp(bands[i].Gain, endBands[i].Gain, i * positioner));
            }
        }
    }
}
