using Cavern.Filters;
using Cavern.Utilities;
using System;
using System.Collections.Generic;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>Generates peaking EQ filter sets that try to match <see cref="Equalizer"/> curves.</summary>
    public class PeakingEqualizer {
        /// <summary>Maximum filter gain in dB.</summary>
        public double MaxGain { get; set; } = 20;

        /// <summary>Minimum filter gain in dB.</summary>
        public double MinGain { get; set; } = -100;

        /// <summary>Round the gain of each filter to this precision..</summary>
        public double GainPrecision { get; set; } = .01;

        /// <summary>Q at the first try.</summary>
        public double StartQ { get; set; } = 10;

        /// <summary>In each iteration, <see cref="StartQ"/> is divided in half, and checks steps in each direction.
        /// The precision of Q will be <see cref="StartQ"/> / 2^<see cref="Iterations"/>.</summary>
        public int Iterations { get; set; } = 8;

        readonly Equalizer source;

        FilterAnalyzer analyzer;

        /// <summary>Generates peaking EQ filter sets that try to match <see cref="Equalizer"/> curves.</summary>
        public PeakingEqualizer(Equalizer source) => this.source = source;

        /// <summary>Measure a filter candidate for <see cref="BruteForceQ(ref float[], double, double)"/>.</summary>
        float BruteForceStep(float[] target, out float[] changedTarget) {
            changedTarget = GraphUtils.ConvertToGraph(analyzer.FrequencyResponse, 20, analyzer.SampleRate * .5,
                analyzer.SampleRate, target.Length);
            GraphUtils.ConvertToDecibels(changedTarget);
            WaveformUtils.Mix(target, changedTarget);
            return QMath.SumAbs(changedTarget);
        }

        /// <summary>Find the filter with the best Q for the given frequency and gain in <paramref name="target"/>.
        /// Correct <paramref name="target"/> to the frequency response with the inverse of the found filter.</summary>
        PeakingEQ BruteForceQ(ref float[] target, double freq, double gain) {
            double q = StartQ, qStep = q * .5;
            gain = Math.Round(QMath.Clamp(-gain, -MaxGain, -MinGain) / GainPrecision) * GainPrecision;
            float targetSum = QMath.SumAbs(target);
            float[] targetSource = (float[])target.Clone();
            for (int i = 0; i < Iterations; ++i) {
                double lowerQ = q - qStep, upperQ = q + qStep;
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, lowerQ, gain));
                float lowerSum = BruteForceStep(targetSource, out float[] lowerTarget);
                if (targetSum > lowerSum) {
                    targetSum = lowerSum;
                    target = lowerTarget;
                    q = lowerQ;
                }
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, upperQ, gain));
                float upperSum = BruteForceStep(targetSource, out float[] upperTarget);
                if (targetSum > upperSum) {
                    targetSum = upperSum;
                    target = upperTarget;
                    q = upperQ;
                }
                qStep *= .5;
            }
            return new PeakingEQ(analyzer.SampleRate, freq, q, -gain);
        }

        /// <summary>Finds a <see cref="PeakingEQ"/> to correct the worst problem on the input spectrum</summary>
        /// <param name="target">Logarithmic input spectrum from 20 to sample rate/2 Hz</param>
        /// <param name="analyzer">A filter analyzer with cached variables that shoudn't be computed again</param>
        /// <remarks><paramref name="target"/> will be corrected to the frequency response with the found filter</remarks>
        PeakingEQ BruteForceBand(ref float[] target, FilterAnalyzer analyzer) {
            IReadOnlyList<Band> bands = source.Bands;
            if (bands.Count == 0)
                return new PeakingEQ(analyzer.SampleRate, 20);
            double startPow = Math.Log10(20), powRange = Math.Log10(analyzer.SampleRate * .5f) - startPow, bandRange = target.Length / powRange;
            int startFreq = (int)((Math.Log10(bands[0].Frequency) - startPow) * bandRange),
                stopFreq = (int)((Math.Log10(bands[bands.Count - 1].Frequency) - startPow) * bandRange);
            if (startFreq < 0)
                startFreq = 0;
            if (stopFreq >= target.Length)
                stopFreq = target.Length - 1;
            float max = Math.Abs(target[startFreq]), abs;
            int maxAt = startFreq;
            for (int i = startFreq + 1; i < stopFreq; ++i) {
                abs = Math.Abs(target[i]);
                if (max < abs) {
                    max = abs;
                    maxAt = i;
                }
            }
            return BruteForceQ(ref target, Math.Pow(10, startPow + powRange * maxAt / target.Length), target[maxAt]);
        }

        /// <summary>Create a peaking EQ filter set with bands placed at optimal frequencies to approximate the drawn EQ curve.</summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate, int bands) {
            float[] target = source.Visualize(20, sampleRate * .5f, 1024);
            PeakingEQ[] result = new PeakingEQ[bands];
            analyzer = new FilterAnalyzer(null, sampleRate);
            for (int band = 0; band < bands; ++band)
                result[band] = BruteForceBand(ref target, analyzer);
            return result;
        }

        /// <summary>Create a peaking EQ filter set with bands placed at equalized frequencies to approximate the drawn EQ curve.</summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate) {
            float[] target = source.Visualize(20, sampleRate * .5f, 1024);
            PeakingEQ[] result = new PeakingEQ[source.Bands.Count];
            for (int band = 0, bandc = source.Bands.Count; band < bandc; ++band)
                result[band] = BruteForceQ(ref target, source.Bands[band].Frequency, source.Bands[band].Gain);
            return result;
        }
    }
}