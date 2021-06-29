using System;

using Cavern.Filters;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>Generates peaking EQ filter sets that try to match <see cref="Equalizer"/> curves.</summary>
    public class PeakingEqualizer {
        /// <summary>Maximum filter gain in dB.</summary>
        public double MaxGain { get; set; } = 20;

        /// <summary>Minimum filter gain in dB.</summary>
        public double MinGain { get; set; } = -100;

        /// <summary>Round the gain of each filter to this precision.</summary>
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
            gain = Math.Round(-QMath.Clamp(gain, MinGain, MaxGain) / GainPrecision) * GainPrecision;
            float targetSum = QMath.SumAbs(target);
            float[] targetSource = target.FastClone();
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

        /// <summary>Finds a <see cref="PeakingEQ"/> to correct the worst problem on the input spectrum.</summary>
        /// <param name="target">Logarithmic input spectrum from 20 to sample rate/2 Hz</param>
        /// <param name="analyzer">A filter analyzer with cached variables that shoudn't be computed again</param>
        /// <param name="startPos">First band to analyze</param>
        /// <param name="stopPos">Last band to analyze</param>
        /// <remarks><paramref name="target"/> will be corrected to the frequency response with the found filter</remarks>
        PeakingEQ BruteForceBand(ref float[] target, FilterAnalyzer analyzer, int startPos, int stopPos) {
            double powRange = Math.Log10(analyzer.SampleRate * .5f) - log10_20;
            float max = Math.Abs(target[startPos]), abs;
            int maxAt = startPos;
            for (int i = startPos + 1; i < stopPos; ++i) {
                abs = Math.Abs(target[i]);
                if (max < abs) {
                    max = abs;
                    maxAt = i;
                }
            }
            return BruteForceQ(ref target, Math.Pow(10, log10_20 + powRange * maxAt / target.Length), target[maxAt]);
        }

        /// <summary>Create a peaking EQ filter set with bands placed at optimal frequencies to approximate the drawn EQ curve.</summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate, int bands) {
            PeakingEQ[] result = new PeakingEQ[bands];
            if (source.Bands.Count == 0) {
                for (int band = 0; band < bands; ++band)
                    result[band] = new PeakingEQ(sampleRate, 20);
                return result;
            }

            float[] target = source.Visualize(20, sampleRate * .5f, 1024);
            double powRange = Math.Log10(sampleRate * .5f) - log10_20, bandRange = target.Length / powRange;
            int startPos = (int)((Math.Log10(source.Bands[0].Frequency) - log10_20) * bandRange),
                stopPos = (int)((Math.Log10(source.Bands[source.Bands.Count - 1].Frequency) - log10_20) * bandRange);
            if (startPos < 0)
                startPos = 0;
            if (stopPos >= target.Length)
                stopPos = target.Length - 1;

            if (CavernAmp.Available) {
                IntPtr extAnalyzer = CavernQuickEQAmp.FilterAnalyzer_Create(sampleRate, MaxGain, MinGain, GainPrecision, StartQ, Iterations);
                for (int band = 0; band < bands; ++band) {
                    CavernAmpPeakingEQ newBand = CavernQuickEQAmp.BruteForceBand(target, target.Length, extAnalyzer, startPos, stopPos);
                    result[band] = new PeakingEQ(sampleRate, newBand.centerFreq, newBand.q, newBand.gain);
                }
                CavernQuickEQAmp.FilterAnalyzer_Dispose(extAnalyzer);
            } else {
                analyzer = new FilterAnalyzer(null, sampleRate);
                for (int band = 0; band < bands; ++band)
                    result[band] = BruteForceBand(ref target, analyzer, startPos, stopPos);
                analyzer.Dispose();
            }
            return result;
        }

        /// <summary>Create a peaking EQ filter set with bands placed at equalized frequencies to approximate the drawn EQ curve.</summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate) {
            float[] target = source.Visualize(20, sampleRate * .5f, 1024);
            PeakingEQ[] result = new PeakingEQ[source.Bands.Count];
            analyzer = new FilterAnalyzer(null, sampleRate);
            for (int band = 0, bandc = source.Bands.Count; band < bandc; ++band)
                result[band] = BruteForceQ(ref target, source.Bands[band].Frequency, source.Bands[band].Gain);
            analyzer.Dispose();
            return result;
        }

        const double log10_20 = 1.3010299956639811952137388947245;
    }
}