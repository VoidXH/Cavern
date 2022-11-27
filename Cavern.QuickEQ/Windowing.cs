using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>
    /// FFT windowing functions.
    /// </summary>
    public static partial class Windowing {
        /// <summary>
        /// Apply a predefined window function on a signal.
        /// </summary>
        /// <param name="samples">Signal to window</param>
        /// <param name="function">Windowing function applied</param>
        public static void ApplyWindow(float[] samples, Window function) =>
            ApplyWindow(samples, function, function, 0, samples.Length / 2, samples.Length);

        /// <summary>
        /// Apply a custom window function on part of a signal.
        /// </summary>
        /// <param name="samples">Signal to window</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        public static void ApplyWindow(float[] samples, Window left, Window right, int start, int splitter, int end) {
            int leftSpan = splitter - start,
                rightSpan = end - splitter,
                endMirror = splitter - (end - splitter),
                posSplitter = Math.Max(splitter, 0);
            float leftSpanDiv = 2 * (float)Math.PI / (leftSpan * 2),
                rightSpanDiv = 2 * (float)Math.PI / (rightSpan * 2);
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                Array.Clear(samples, 0, start);
                for (int sample = Math.Max(start, 0), actEnd = Math.Min(posSplitter, samples.Length); sample < actEnd; ++sample) {
                    samples[sample] *= leftFunc((sample - start) * leftSpanDiv);
                }
            }
            if (right != Window.Disabled) {
                if (end < 0) {
                    end = 0;
                }
                WindowFunction rightFunc = GetWindowFunction(right);
                for (int sample = posSplitter, actEnd = Math.Min(end, samples.Length); sample < actEnd; ++sample) {
                    samples[sample] *= rightFunc((sample - endMirror) * rightSpanDiv);
                }
                Array.Clear(samples, end, samples.Length - end);
            }
        }

        /// <summary>
        /// Apply a custom window function on part of a multichannel signal.
        /// </summary>
        /// <param name="samples">Signal to window</param>
        /// <param name="channels">Channel count</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        public static void ApplyWindow(float[] samples, int channels, Window left, Window right, int start, int splitter, int end) {
            int leftSpan = splitter - start,
                rightSpan = end - splitter,
                endMirror = splitter - (end - splitter),
                posSplitter = Math.Max(splitter, 0);
            float leftSpanDiv = 2 * channels * (float)Math.PI / (leftSpan * 2),
                rightSpanDiv = 2 * channels * (float)Math.PI / (rightSpan * 2);
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                Array.Clear(samples, 0, start);
                for (int sample = Math.Max(start, 0), actEnd = Math.Min(posSplitter, samples.Length); sample < actEnd; ++sample) {
                    samples[sample] *= leftFunc((sample - start) / channels * leftSpanDiv);
                }
            }
            if (right != Window.Disabled) {
                if (end < 0) {
                    end = 0;
                }
                WindowFunction rightFunc = GetWindowFunction(right);
                for (int sample = posSplitter, actEnd = Math.Min(end, samples.Length); sample < actEnd; ++sample) {
                    samples[sample] *= rightFunc((sample - endMirror) / channels * rightSpanDiv);
                }
                Array.Clear(samples, end, samples.Length - end);
            }
        }

        /// <summary>
        /// Apply a predefined window function on a signal.
        /// </summary>
        /// <param name="samples">Measurement to window</param>
        /// <param name="function">Windowing function applied</param>
        public static void ApplyWindow(Complex[] samples, Window function) =>
            ApplyWindow(samples, function, function, 0, samples.Length / 2, samples.Length);

        /// <summary>
        /// Apply a custom window function on part of a signal.
        /// </summary>
        /// <param name="samples">Measurement to window</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        public static void ApplyWindow(Complex[] samples, Window left, Window right, int start, int splitter, int end) {
            int leftSpan = splitter - start,
                rightSpan = end - splitter,
                endMirror = splitter - (end - splitter),
                posSplitter = Math.Max(splitter, 0);
            float leftSpanDiv = 2 * (float)Math.PI / (leftSpan * 2),
                rightSpanDiv = 2 * (float)Math.PI / (rightSpan * 2);
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                for (int sample = 0; sample < start; ++sample) {
                    samples[sample] = new Complex();
                }
                for (int sample = Math.Max(start, 0), actEnd = Math.Min(posSplitter, samples.Length); sample < actEnd; ++sample) {
                    float mul = leftFunc((sample - start) * leftSpanDiv);
                    samples[sample].Real *= mul;
                    samples[sample].Imaginary *= mul;
                }
            }
            if (right != Window.Disabled) {
                int posEnd = Math.Max(end, 0);
                WindowFunction rightFunc = GetWindowFunction(right);
                for (int sample = posSplitter, actEnd = Math.Min(posEnd, samples.Length); sample < actEnd; ++sample) {
                    float mul = rightFunc((sample - endMirror) * rightSpanDiv);
                    samples[sample].Real *= mul;
                    samples[sample].Imaginary *= mul;
                }
                for (int sample = posEnd, actEnd = samples.Length; sample < actEnd; ++sample) {
                    samples[sample] = new Complex();
                }
            }
        }

        /// <summary>
        /// Add windowing on the right of the curve. Windowing is applied logarithmically.
        /// </summary>
        public static void ApplyWindow(List<Band> bands, Window right, double startFreq, double endFreq) {
            startFreq = Math.Log10(startFreq);
            endFreq = Math.Log10(endFreq);
            double range = Math.PI / (endFreq - startFreq);

            int i = 0, c = bands.Count;
            while (i < c && Math.Log10(bands[i].Frequency) < startFreq) {
                i++;
            }

            WindowFunctionDouble function = GetWindowFunctionDouble(right);
            for (; i < c; i++) {
                double logFreq = Math.Log10(bands[i].Frequency);
                if (logFreq > endFreq) {
                    break;
                }

                bands[i] *= function(Math.PI + (logFreq - startFreq) * range);
            }

            for (; i < c; i++) {
                bands[i] = new Band(bands[i].Frequency, 0);
            }
        }
    }
}