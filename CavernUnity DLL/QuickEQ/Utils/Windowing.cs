﻿using System;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Available FFT windows.</summary>
    public enum Window {
        /// <summary>1</summary>
        Rectangular,
        /// <summary>sin(x)</summary>
        Sine,
        /// <summary>0.54 - 0.46 * cos(x)</summary>
        Hamming,
        /// <summary>0.5 * (1 - cos(x))</summary>
        Hann,
        /// <summary>0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)</summary>
        Blackman,
        /// <summary>0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)</summary>
        BlackmanHarris,
        /// <summary>A window for impulse response trimming, with a precompiled alpha.</summary>
        Tukey
    }

    /// <summary>FFT windowing functions.</summary>
    public static class Windowing {
        /// <summary>Apply a predefined window function on a signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Function">Windowing function applied</param>
        public static void ApplyWindow(float[] Samples, Window Function) {
            WindowFunction Windowing = GetWindowFunction(Function);
            ApplyWindow(Samples, Windowing, Windowing, 0, Samples.Length / 2, Samples.Length);
        }

        /// <summary>Apply a custom window function on part of a signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Left">Window function left from the marker</param>
        /// <param name="Right">Window function right from the marker</param>
        /// <param name="Start">Beginning of the window in samples</param>
        /// <param name="Splitter">The point where the two window functions change</param>
        /// <param name="End">End of the window in samples</param>
        public static void ApplyWindow(float[] Samples, Window Left, Window Right, int Start, int Splitter, int End) {
            ApplyWindow(Samples, GetWindowFunction(Left), GetWindowFunction(Right), Start, Splitter, End);
        }

        /// <summary>Implementation of <see cref="ApplyWindow(float[], Window, Window, int, int, int)"/>.</summary>
        static void ApplyWindow(float[] Samples, WindowFunction Left, WindowFunction Right, int Start, int Splitter, int End) {
            int LeftSpan = Splitter - Start, RightSpan = End - Splitter, EndMirror = Splitter - (End - Splitter);
            float LeftSpanDiv = Measurements.Pix2 / (LeftSpan * 2), RightSpanDiv = Measurements.Pix2 / (RightSpan * 2);
            for (int Sample = Math.Max(Start, 0); Sample < Splitter; ++Sample)
                Samples[Sample] *= Left((Sample - Start) * LeftSpanDiv);
            for (int Sample = Splitter, ActEnd = Math.Min(End, Samples.Length); Sample < ActEnd; ++Sample)
                Samples[Sample] *= Right((Sample - EndMirror) * RightSpanDiv);
        }

        /// <summary>Apply a predefined window function on a signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Function">Windowing function applied</param>
        public static void ApplyWindow(Complex[] Samples, Window Function) {
            WindowFunction Windowing = GetWindowFunction(Function);
            ApplyWindow(Samples, Windowing, Windowing, 0, Samples.Length / 2, Samples.Length);
        }

        /// <summary>Apply a custom window function on part of a signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Left">Window function left from the marker</param>
        /// <param name="Right">Window function right from the marker</param>
        /// <param name="Start">Beginning of the window in samples</param>
        /// <param name="Splitter">The point where the two window functions change</param>
        /// <param name="End">End of the window in samples</param>
        public static void ApplyWindow(Complex[] Samples, Window Left, Window Right, int Start, int Splitter, int End) {
            ApplyWindow(Samples, GetWindowFunction(Left), GetWindowFunction(Right), Start, Splitter, End);
        }

        /// <summary>Implementation of <see cref="ApplyWindow(Complex[], Window, Window, int, int, int)"/>.</summary>
        static void ApplyWindow(Complex[] Samples, WindowFunction Left, WindowFunction Right, int Start, int Splitter, int End) {
            int LeftSpan = Splitter - Start, RightSpan = End - Splitter, EndMirror = Splitter - (End - Splitter);
            float LeftSpanDiv = Measurements.Pix2 / (LeftSpan * 2), RightSpanDiv = Measurements.Pix2 / (RightSpan * 2);
            for (int Sample = Math.Max(Start, 0); Sample < Splitter; ++Sample)
                Samples[Sample] *= Left((Sample - Start) * LeftSpanDiv);
            for (int Sample = Splitter, ActEnd = Math.Min(End, Samples.Length); Sample < ActEnd; ++Sample)
                Samples[Sample] *= Right((Sample - EndMirror) * RightSpanDiv);
        }

        /// <summary>Window function format.</summary>
        /// <param name="x">The position in the signal from 0 to 2 * pi</param>
        /// <returns>The multiplier for the sample at x</returns>
        delegate float WindowFunction(float x);

        /// <summary>Get the corresponding window function for each <see cref="Window"/> value.</summary>
        static WindowFunction GetWindowFunction(Window Function) {
            switch (Function) {
                case Window.Sine: return SineWindow;
                case Window.Hamming: return HammingWindow;
                case Window.Hann: return HannWindow;
                case Window.Blackman: return BlackmanWindow;
                case Window.BlackmanHarris: return BlackmanHarrisWindow;
                case Window.Tukey: return TukeyWindow;
                default: return x => 1;
            }
        }

        /// <summary>sin(x)</summary>
        static float SineWindow(float x) { return Mathf.Sin(x * .5f); }
        /// <summary>0.54 - 0.46 * cos(x)</summary>
        static float HammingWindow(float x) { return .54f - .46f * Mathf.Cos(x); }
        /// <summary>0.5 * (1 - cos(x))</summary>
        static float HannWindow(float x) { return .5f * (1 - Mathf.Cos(x)); }
        /// <summary>0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)</summary>
        static float BlackmanWindow(float x) { return .42f - .5f * Mathf.Cos(x) + .08f * Mathf.Cos(x + x); }
        /// <summary>0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)</summary>
        static float BlackmanHarrisWindow(float x) {
            float x2 = x + x;
            return .35875f - .48829f * Mathf.Cos(x) + .14128f * Mathf.Cos(x2) - .01168f * Mathf.Cos(x2 + x);
        }
        /// <summary>A window for impulse response trimming, with a precompiled alpha.</summary>
        static float TukeyWindow(float x) {
            const float Alpha = .25f, Positioner = 1 / Alpha, FlatLeft = Mathf.PI * Alpha, FlatRight = Mathf.PI * (2 - Alpha);
            if (x < FlatLeft)
                return (Mathf.Cos(x * Positioner - Mathf.PI) + 1) * .5f;
            else if (x > FlatRight)
                return (Mathf.Cos((Measurements.Pix2 - x) * Positioner - Mathf.PI) + 1) * .5f;
            else
                return 1;
        }
    }
}