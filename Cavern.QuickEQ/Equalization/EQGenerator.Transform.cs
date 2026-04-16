using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class EQGenerator {
        /// <summary>
        /// Fade the <paramref name="low"/> frequencies into the <paramref name="high"/>s around the <paramref name="transitionFreq"/>.
        /// </summary>
        /// <param name="low">Curve to take the low frequency values of</param>
        /// <param name="high">Curve to take the high frequency values of, the frequencies must match the <paramref name="low"/></param>
        /// <param name="transitionFreq">The point where both curves contribute equally</param>
        /// <param name="transitionSpan">In octaves, the width of the transition region</param>
        public static Equalizer Fade(Equalizer low, Equalizer high, double transitionFreq, double transitionSpan) {
            List<Band> output = new List<Band>();
            double transitionRange = Math.Pow(2, transitionSpan * .5);
            (int startBand, int endBand) = low.GetBandLimits(transitionFreq / transitionRange, transitionFreq * transitionRange);
            if (startBand == -1) {
                return (Equalizer)high.Clone();
            }

            IReadOnlyList<Band> lowBands = low.Bands,
                highBands = high.Bands;
            for (int i = 0; i < startBand; i++) {
                output.Add(lowBands[i]);
            }
            for (int i = startBand; i < endBand; i++) {
                double ratio = QMath.LerpInverse(startBand, endBand, i);
                output.Add(new Band(lowBands[i].Frequency, QMath.Lerp(lowBands[i].Gain, highBands[i].Gain, ratio)));
            }
            for (int i = endBand, c = highBands.Count; i < c; i++) {
                output.Add(highBands[i]);
            }
            return new Equalizer(output, true);
        }

        /// <summary>
        /// Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.
        /// The default of 2 octave smoothing will be used.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PeakingEQ[] GetPeakingEQ(this Equalizer eq, int sampleRate) => eq.GetPeakingEQ(sampleRate, 2);

        /// <summary>
        /// Create a peaking EQ filter set with bands at the positions of the EQ's bands to approximate the drawn EQ curve.
        /// </summary>
        /// <param name="eq">Source <see cref="Equalizer"/></param>
        /// <param name="sampleRate">Target system sample rate</param>
        /// <param name="smoothing">Smooth out band spikes</param>
        public static PeakingEQ[] GetPeakingEQ(this Equalizer eq, int sampleRate, double smoothing) {
            IReadOnlyList<Band> bands = eq.Bands;
            PeakingEQ[] result = new PeakingEQ[bands.Count];
            double freq = bands[0].Frequency, gainMul = 1 / Math.Pow(smoothing, QFactor.reference), qMul = Math.PI / smoothing;
            if (result.Length > 1) {
                result[0] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, (bands[1].Frequency - freq) * 2) * qMul,
                    bands[0].Gain * gainMul);
                int end = result.Length - 1;
                for (int band = 1; band < end; ++band) {
                    freq = bands[band].Frequency;
                    result[band] = new PeakingEQ(sampleRate, freq, QFactor.FromBandwidth(freq, bands[band - 1].Frequency,
                        bands[band + 1].Frequency) * qMul, bands[band].Gain * gainMul);
                }
                freq = bands[end].Frequency;
                result[end] = new PeakingEQ(sampleRate, freq,
                    QFactor.FromBandwidth(freq, (freq - bands[end - 1].Frequency) * 2) * qMul, bands[end].Gain * gainMul);
            } else if (result.Length == 1) {
                result[0] = new PeakingEQ(sampleRate, freq, .001f, bands[0].Gain);
            }
            return result;
        }
    }
}
