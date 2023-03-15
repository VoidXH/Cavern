using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
            ApplyWindow(samples, 1, function, function, 0, samples.Length / 2, samples.Length);

        /// <summary>
        /// Apply a custom window function on part of a mono signal.
        /// </summary>
        /// <param name="samples">Signal to window</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyWindow(float[] samples, Window left, Window right, int start, int splitter, int end) =>
            ApplyWindow(samples, 1, left, right, start, splitter, end);

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
            if (splitter < 0) {
                splitter = 0;
            }
            if (left != Window.Disabled) {
                Array.Clear(samples, 0, start);
                ApplyHalfWindow(samples, channels, start, splitter, left);
            }
            if (right != Window.Disabled) {
                ApplyHalfWindow(samples, channels, end, splitter, right);
                Array.Clear(samples, end, samples.Length - end);
            }
        }

        /// <summary>
        /// Apply half of a window on part of a signal. To make the window fade out instead of fading in,
        /// switch <paramref name="from"/> and <paramref name="to"/>.
        /// </summary>
        public static void ApplyHalfWindow(float[] samples, int channels, int from, int to, Window function) {
            if (from < 0) {
                from = 0;
            }
            if (to > samples.Length) {
                to = samples.Length;
            }
            WindowFunction fptr = GetWindowFunction(function);
            float offset = 0;
            if (from > to) {
                (from, to) = (to, from);
                offset = MathF.PI;
            }
            float step = channels * MathF.PI / (to - from);
            for (int channel = 0; channel < channels; channel++) {
                for (int sample = from + channel; sample < to; sample += channels) {
                    samples[sample] *= fptr(offset);
                    offset += step;
                }
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
            float leftSpanDiv = MathF.PI / leftSpan,
                rightSpanDiv = MathF.PI / rightSpan;
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                Array.Clear(samples, 0, start);
                for (int sample = Math.Max(start, 0), actEnd = Math.Min(posSplitter, samples.Length); sample < actEnd; sample++) {
                    float mul = leftFunc((sample - start) * leftSpanDiv);
                    samples[sample] *= mul;
                }
            }
            if (right != Window.Disabled) {
                int posEnd = Math.Max(end, 0);
                WindowFunction rightFunc = GetWindowFunction(right);
                for (int sample = posSplitter, actEnd = Math.Min(posEnd, samples.Length); sample < actEnd; sample++) {
                    float mul = rightFunc((sample - endMirror) * rightSpanDiv);
                    samples[sample] *= mul;
                }
                Array.Clear(samples, posEnd, samples.Length - end);
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