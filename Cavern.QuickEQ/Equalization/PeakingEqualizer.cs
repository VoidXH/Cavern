using System;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Generates peaking EQ filter sets that try to match <see cref="Equalizer"/> curves.
    /// </summary>
    public class PeakingEqualizer {
        /// <summary>
        /// Highest allowed frequency to place a filter at.
        /// </summary>
        public double MaxFrequency { get; set; } = 16000;

        /// <summary>
        /// Lowest allowed frequency to place a filter at.
        /// </summary>
        public double MinFrequency { get; set; } = 20;

        /// <summary>
        /// Maximum filter gain in dB.
        /// </summary>
        public double MaxGain { get; set; } = 20;

        /// <summary>
        /// Minimum filter gain in dB.
        /// </summary>
        public double MinGain { get; set; } = -100;

        /// <summary>
        /// Round the gain of each filter to this precision.
        /// </summary>
        public double GainPrecision { get; set; } = .01;

        /// <summary>
        /// Q at the first try.
        /// </summary>
        public double StartQ { get; set; } = 10;

        /// <summary>
        /// In each iteration, <see cref="StartQ"/> is divided in half, and checks steps in each direction.
        /// The precision of Q will be <see cref="StartQ"/> / 2^<see cref="Iterations"/>.
        /// </summary>
        public int Iterations { get; set; } = 8;

        readonly Equalizer source;

        /// <summary>
        /// Base-10 logarithm of <see cref="MaxFrequency"/>.
        /// </summary>
        double logMaxFreq;

        /// <summary>
        /// Base-10 logarithm of <see cref="MinFrequency"/>.
        /// </summary>
        double logMinFreq;

        FilterAnalyzer analyzer;

        /// <summary>
        /// Generates peaking EQ filter sets that try to match <see cref="Equalizer"/> curves.
        /// </summary>
        public PeakingEqualizer(Equalizer source) => this.source = source;

        /// <summary>
        /// Create a peaking EQ filter set with bands placed at optimal frequencies to approximate the drawn EQ curve.
        /// </summary>
        /// <remarks>Might return less <paramref name="bands"/> when no better solution can be found in the iteration limit.</remarks>
        public PeakingEQ[] GetPeakingEQ(int sampleRate, int bands) {
            PeakingEQ[] result = new PeakingEQ[bands];
            if (source.Bands.Count == 0) {
                for (int band = 0; band < bands; band++) {
                    result[band] = new PeakingEQ(sampleRate, MinFrequency);
                }
                return result;
            }

            logMinFreq = Math.Log10(MinFrequency);
            logMaxFreq = Math.Log10(MaxFrequency);
            float[] target;
            double bandRange;
            int finalPos;
            if (CavernAmp.Available) { // CavernAmp always works up to the Nyquist frequency
                target = source.Visualize(MinFrequency, sampleRate * .5f, 1024);
                double logPos = Math.Log10(sampleRate * .5f);
                bandRange = target.Length / (logPos - logMinFreq);
                finalPos = Math.Min((int)((logPos - logMinFreq) * bandRange), target.Length);
            } else {
                target = source.Visualize(MinFrequency, MaxFrequency, 1024);
                bandRange = target.Length / (logMaxFreq - logMinFreq);
                finalPos = target.Length;
            }
            int startPos = (int)((Math.Log10(source.Bands[0].Frequency) - logMinFreq) * bandRange),
                stopPos = (int)((Math.Log10(source.Bands[source.Bands.Count - 1].Frequency) - logMinFreq) * bandRange);

            if (startPos < 0) {
                startPos = 0;
            }
            if (stopPos >= finalPos) {
                stopPos = finalPos - 1;
            }

            if (CavernAmp.Available) {
                IntPtr extAnalyzer =
                    CavernQuickEQAmp.FilterAnalyzer_Create(sampleRate, MaxGain, MinGain, GainPrecision, StartQ, Iterations);
                for (int band = 0; band < bands; band++) {
                    CavernAmpPeakingEQ newBand = CavernQuickEQAmp.BruteForceBand(target, target.Length, extAnalyzer, startPos, stopPos);
                    result[band] = new PeakingEQ(sampleRate, newBand.centerFreq, newBand.q, newBand.gain);
                }
                CavernQuickEQAmp.FilterAnalyzer_Dispose(extAnalyzer);
            } else {
                analyzer = new FilterAnalyzer(null, sampleRate);
                for (int band = 0; band < bands; band++) {
                    result[band] = BruteForceBand(ref target, startPos, stopPos);
                    if (result[band] == null) {
                        return result[..band];
                    }
                }
                analyzer.Dispose();
            }
            return Cleanup(result);
        }

        /// <summary>
        /// Create a peaking EQ filter set with bands placed at equalized frequencies to approximate the drawn EQ curve.
        /// </summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate) {
            float[] target = source.Visualize(MinFrequency, MaxFrequency, 1024);
            PeakingEQ[] result = new PeakingEQ[source.Bands.Count];
            analyzer = new FilterAnalyzer(null, sampleRate);
            for (int band = 0, bandc = source.Bands.Count; band < bandc; band++) {
                result[band] = BruteForceQ(ref target, source.Bands[band].Frequency, source.Bands[band].Gain);
            }
            analyzer.Dispose();
            return Cleanup(result);
        }

        /// <summary>
        /// Create a peaking EQ filter set with constant bandwidth between the frequencies. This mimics legacy x-band EQs.
        /// </summary>
        public PeakingEQ[] GetPeakingEQ(int sampleRate, double firstBand, int bandsPerOctave, int bands) {
            float[] target = source.Visualize(MinFrequency, MaxFrequency, 1024);
            PeakingEQ[] result = new PeakingEQ[bands];
            double bandwidth = 1.0 / bandsPerOctave;
            double q = QFactor.FromBandwidth(bandwidth);
            analyzer = new FilterAnalyzer(null, sampleRate);
            for (int i = 0; i < bands; i++) {
                double freq = firstBand * Math.Pow(2, i * bandwidth);
                result[i] = BruteForceGain(ref target, freq, q);
            }
            analyzer.Dispose();
            return Cleanup(result);
        }

        /// <summary>
        /// Measure a filter candidate for <see cref="BruteForceQ(ref float[], double, double, bool)"/>.
        /// </summary>
        float BruteForceStep(float[] target, out float[] changedTarget) {
            changedTarget = GraphUtils.ConvertToGraph(analyzer.FrequencyResponse, MinFrequency, MaxFrequency,
                analyzer.SampleRate, target.Length);
            GraphUtils.ConvertToDecibels(changedTarget);
            WaveformUtils.Mix(target, changedTarget);
            return changedTarget.SumAbs();
        }

        /// <summary>
        /// When the EQ generation finishes, the last band repeats. This function returns an array of results
        /// with the invalid entries removed.
        /// </summary>
        PeakingEQ[] Cleanup(PeakingEQ[] results) {
            PeakingEQ last = results[^1];
            for (int i = 0; i < results.Length - 1; i++) {
                if (results[i].CenterFreq == last.CenterFreq && results[i].Gain == last.Gain && results[i].Q == last.Q) {
                    return results[..i];
                }
            }
            return results;
        }

        /// <summary>
        /// Find the filter with the best Q for the given frequency and gain in <paramref name="target"/>.
        /// Correct <paramref name="target"/> to the frequency response with the inverse of the found filter.
        /// </summary>
        /// <returns>The found filter if it's required, or null if it isn't</returns>
        PeakingEQ BruteForceGain(ref float[] target, double freq, double q) {
            float targetSum = target.SumAbs();
            double gainStep = (MaxGain - MinGain) * .5,
                gain = MinGain + gainStep;
            float[] targetSource = target.FastClone();
            for (int i = 0; i < Iterations; i++) {
                gainStep *= .5;
                double lowerGain = gain - gainStep, upperGain = gain + gainStep;
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, q, lowerGain));
                float lowerSum = BruteForceStep(targetSource, out float[] lowerTarget);
                if (targetSum > lowerSum) {
                    targetSum = lowerSum;
                    target = lowerTarget;
                    gain = lowerGain;
                }
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, q, upperGain));
                float upperSum = BruteForceStep(targetSource, out float[] upperTarget);
                if (targetSum > upperSum) {
                    targetSum = upperSum;
                    target = upperTarget;
                    gain = upperGain;
                }
            }
            return new PeakingEQ(analyzer.SampleRate, freq, q, SnapGain(gain));
        }

        /// <summary>
        /// Find the filter with the best Q for the given frequency and gain in <paramref name="target"/>.
        /// Correct <paramref name="target"/> to the frequency response with the inverse of the found filter.
        /// </summary>
        /// <returns>The found filter if it's required, or null if it isn't</returns>
        PeakingEQ BruteForceQ(ref float[] target, double freq, double gain, bool alwaysValid = false) {
            double q = StartQ,
                qStep = q * .5;
            gain = Math.Round(-Math.Clamp(gain, MinGain, MaxGain) / GainPrecision) * GainPrecision;
            float targetSum = target.SumAbs();
            float[] targetSource = target.FastClone();
            bool valid = alwaysValid; // If false, we're better off without this filter
            for (int i = 0; i < Iterations; i++) {
                double lowerQ = q - qStep, upperQ = q + qStep;
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, lowerQ, gain));
                float lowerSum = BruteForceStep(targetSource, out float[] lowerTarget);
                if (targetSum > lowerSum) {
                    targetSum = lowerSum;
                    target = lowerTarget;
                    q = lowerQ;
                    valid = true;
                }
                analyzer.Reset(new PeakingEQ(analyzer.SampleRate, freq, upperQ, gain));
                float upperSum = BruteForceStep(targetSource, out float[] upperTarget);
                if (targetSum > upperSum) {
                    targetSum = upperSum;
                    target = upperTarget;
                    q = upperQ;
                    valid = true;
                }
                qStep *= .5;
            }
            return valid ? new PeakingEQ(analyzer.SampleRate, freq, q, -gain) : null;
        }

        /// <summary>
        /// Finds a <see cref="PeakingEQ"/> to correct the worst problem on the input spectrum.
        /// </summary>
        /// <param name="target">Logarithmic input spectrum between <see cref="MinFrequency"/> and <see cref="MaxFrequency"/></param>
        /// <param name="startPos">First band to analyze</param>
        /// <param name="stopPos">Last band to analyze</param>
        /// <remarks><paramref name="target"/> will be corrected to the frequency response with the found filter</remarks>
        PeakingEQ BruteForceBand(ref float[] target, int startPos, int stopPos) {
            float max = Math.Abs(target[startPos]), abs;
            int maxAt = startPos;
            for (int i = startPos + 1; i < stopPos; i++) {
                abs = Math.Abs(target[i]);
                if (max < abs) {
                    max = abs;
                    maxAt = i;
                }
            }
            return BruteForceQ(ref target, Math.Pow(10, logMinFreq + (logMaxFreq - logMinFreq) * maxAt / target.Length), target[maxAt]);
        }

        /// <summary>
        /// Sets the requested gain to a value that's permitted by the respective parameters
        /// (<see cref="MinGain"/>, <see cref="MaxGain"/>, and <see cref="GainPrecision"/>).
        /// </summary>
        double SnapGain(double gain) => Math.Round(-Math.Clamp(gain, MinGain, MaxGain) / GainPrecision) * GainPrecision;
    }
}