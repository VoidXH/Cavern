using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Complex-domain equalization utilities.
    /// </summary>
    public static class ComplexEqualization {
        /// <summary>
        /// Apply a rolling-window average to a complex transfer function in both dimensions.
        /// </summary>
        /// <param name="source">The complex transfer function to smooth.</param>
        /// <param name="sampleRate">Sample rate of the transfer function.</param>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <returns>A new smoothed complex transfer function.</returns>
        public static Complex[] Smooth(Complex[] source, int sampleRate, double octaves)
            => Smooth(source, sampleRate, 0, sampleRate / 2.0, octaves, octaves);

        /// <summary>
        /// Apply a rolling-window average to a complex transfer function with a window size that changes by frequency.
        /// </summary>
        /// <param name="source">The complex transfer function to smooth.</param>
        /// <param name="sampleRate">Sample rate of the transfer function.</param>
        /// <param name="startOctaves">Smoothing window size in octaves at the beginning of the spectrum.</param>
        /// <param name="endOctaves">Smoothing window size in octaves at the end of the spectrum.</param>
        /// <returns>A new smoothed complex transfer function.</returns>
        public static Complex[] Smooth(Complex[] source, int sampleRate, double startOctaves, double endOctaves)
            => Smooth(source, sampleRate, 0, sampleRate / 2.0, startOctaves, endOctaves);

        /// <summary>
        /// Apply a rolling-window average to a complex transfer function in both dimensions, limited to a frequency range.
        /// </summary>
        /// <param name="source">The complex transfer function to smooth.</param>
        /// <param name="sampleRate">Sample rate of the transfer function.</param>
        /// <param name="startFreq">Minimum frequency to smooth (Hz).</param>
        /// <param name="endFreq">Maximum frequency to smooth (Hz).</param>
        /// <param name="octaves">Width of the rolling window in octaves.</param>
        /// <returns>A new smoothed complex transfer function.</returns>
        public static Complex[] Smooth(Complex[] source, int sampleRate, double startFreq, double endFreq, double octaves)
            => Smooth(source, sampleRate, startFreq, endFreq, octaves, octaves);

        /// <summary>
        /// Apply a rolling-window average to a complex transfer function in both dimensions, limited to a frequency range with variable window size.
        /// </summary>
        /// <param name="source">The complex transfer function to smooth.</param>
        /// <param name="sampleRate">Sample rate of the transfer function.</param>
        /// <param name="startFreq">Minimum frequency to smooth (Hz).</param>
        /// <param name="endFreq">Maximum frequency to smooth (Hz).</param>
        /// <param name="startOctaves">Smoothing window size in octaves at <paramref name="startFreq"/>.</param>
        /// <param name="endOctaves">Smoothing window size in octaves at <paramref name="endFreq"/>.</param>
        /// <returns>A new smoothed complex transfer function.</returns>
        public static Complex[] Smooth(Complex[] source, int sampleRate, double startFreq, double endFreq, double startOctaves, double endOctaves) {
            int length = source.Length;
            Complex[] result = new Complex[length];
            double step = (double)sampleRate / (length - 1);

            float[] prefixMag = source.PrefixSumMagnitudes();
            Complex[] prefixDir = source.PrefixSumDirections();

            int smoothFrom = 0;
            int smoothTo = 0;
            double effectiveStartFreq = startFreq > 0 ? startFreq : step;

            for (int i = 0; i < length; i++) {
                double freq = step * i;
                if (freq < startFreq || freq > endFreq) {
                    result[i] = source[i];
                    continue;
                }

                double position = Math.Clamp(QMath.LorpInverse(effectiveStartFreq, endFreq, freq), 0.0, 1.0);
                double octaves = QMath.Lerp(startOctaves, endOctaves, position);
                double multipleTo = Math.Pow(2, octaves);
                double multipleFrom = 1 / multipleTo;

                double minFreq = Math.Max(freq * multipleFrom, startFreq);
                double maxFreq = Math.Min(freq * multipleTo, endFreq);

                while (smoothTo < length && step * smoothTo < maxFreq) {
                    smoothTo++;
                }

                while (smoothFrom < length && step * smoothFrom < minFreq) {
                    smoothFrom++;
                }

                int windowSize = smoothTo - smoothFrom;
                if (windowSize > 0) {
                    float magMean = (prefixMag[smoothTo] - prefixMag[smoothFrom]) / windowSize;
                    Complex dirSum = prefixDir[smoothTo] - prefixDir[smoothFrom];
                    float dirMag = dirSum.Magnitude;
                    if (dirMag > 0) {
                        result[i] = dirSum * (magMean / dirMag);
                    } else {
                        result[i] = new Complex(magMean);
                    }
                } else {
                    result[i] = source[i];
                }
            }

            return result;
        }
    }
}
