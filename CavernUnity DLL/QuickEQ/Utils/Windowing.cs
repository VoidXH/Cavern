using System;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Available FFT windows.</summary>
    public enum Window {
        /// <summary>No windowing.</summary>
        Disabled,
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
        /// <param name="samples">Measurement to window</param>
        /// <param name="function">Windowing function applied</param>
        public static void ApplyWindow(float[] samples, Window function) => ApplyWindow(samples, function, function, 0, samples.Length / 2, samples.Length);

        /// <summary>Apply a custom window function on part of a signal.</summary>
        /// <param name="samples">Measurement to window</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        public static void ApplyWindow(float[] samples, Window left, Window right, int start, int splitter, int end) {
            int leftSpan = splitter - start, rightSpan = end - splitter, endMirror = splitter - (end - splitter), posSplitter = Math.Max(splitter, 0);
            float leftSpanDiv = 2 * Mathf.PI / (leftSpan * 2), rightSpanDiv = 2 * Mathf.PI / (rightSpan * 2);
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                Array.Clear(samples, 0, start);
                for (int sample = Math.Max(start, 0), actEnd = Math.Min(posSplitter, samples.Length); sample < actEnd; ++sample)
                    samples[sample] *= leftFunc((sample - start) * leftSpanDiv);
            }
            if (right != Window.Disabled) {
                int posEnd = Math.Max(end, 0);
                WindowFunction rightFunc = GetWindowFunction(right);
                for (int sample = posSplitter, actEnd = Math.Min(posEnd, samples.Length); sample < actEnd; ++sample)
                    samples[sample] *= rightFunc((sample - endMirror) * rightSpanDiv);
                Array.Clear(samples, posEnd, samples.Length - posEnd);
            }
        }

        /// <summary>Apply a predefined window function on a signal.</summary>
        /// <param name="samples">Measurement to window</param>
        /// <param name="function">Windowing function applied</param>
        public static void ApplyWindow(Complex[] samples, Window function) => ApplyWindow(samples, function, function, 0, samples.Length / 2, samples.Length);

        /// <summary>Apply a custom window function on part of a signal.</summary>
        /// <param name="samples">Measurement to window</param>
        /// <param name="left">Window function left from the marker</param>
        /// <param name="right">Window function right from the marker</param>
        /// <param name="start">Beginning of the window in samples</param>
        /// <param name="splitter">The point where the two window functions change</param>
        /// <param name="end">End of the window in samples</param>
        public static void ApplyWindow(Complex[] samples, Window left, Window right, int start, int splitter, int end) {
            int leftSpan = splitter - start, rightSpan = end - splitter, endMirror = splitter - (end - splitter), posSplitter = Math.Max(splitter, 0);
            float leftSpanDiv = 2 * Mathf.PI / (leftSpan * 2), rightSpanDiv = 2 * Mathf.PI / (rightSpan * 2);
            if (left != Window.Disabled) {
                WindowFunction leftFunc = GetWindowFunction(left);
                for (int sample = 0; sample < start; ++sample)
                    samples[sample] = new Complex();
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
                for (int sample = posEnd, actEnd = samples.Length; sample < actEnd; ++sample)
                    samples[sample] = new Complex();
            }
        }

        /// <summary>Window function format.</summary>
        /// <param name="x">The position in the signal from 0 to 2 * pi</param>
        /// <returns>The multiplier for the sample at x</returns>
        delegate float WindowFunction(float x);

        /// <summary>Get the corresponding window function for each <see cref="Window"/> value.</summary>
        static WindowFunction GetWindowFunction(Window function) {
            switch (function) {
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
        static float SineWindow(float x) => Mathf.Sin(x * .5f);
        /// <summary>0.54 - 0.46 * cos(x)</summary>
        static float HammingWindow(float x) => .54f - .46f * Mathf.Cos(x);
        /// <summary>0.5 * (1 - cos(x))</summary>
        static float HannWindow(float x) => .5f * (1 - Mathf.Cos(x));
        /// <summary>0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)</summary>
        static float BlackmanWindow(float x) => .42f - .5f * Mathf.Cos(x) + .08f * Mathf.Cos(x + x);
        /// <summary>0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)</summary>
        static float BlackmanHarrisWindow(float x) {
            float x2 = x + x;
            return .35875f - .48829f * Mathf.Cos(x) + .14128f * Mathf.Cos(x2) - .01168f * Mathf.Cos(x2 + x);
        }
        /// <summary>A window for impulse response trimming, with a precompiled alpha.</summary>
        static float TukeyWindow(float x) {
            const float alpha = .25f, positioner = 1 / alpha, flatLeft = Mathf.PI * alpha, flatRight = Mathf.PI * (2 - alpha);
            if (x < flatLeft)
                return (Mathf.Cos(x * positioner - Mathf.PI) + 1) * .5f;
            else if (x > flatRight)
                return (Mathf.Cos((2 * Mathf.PI - x) * positioner - Mathf.PI) + 1) * .5f;
            else
                return 1;
        }
    }
}