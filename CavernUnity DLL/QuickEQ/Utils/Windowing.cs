using System;
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
        /// <summary>A window designed to flatten sweep responses.</summary>
        Void
    }

    /// <summary>FFT windowing functions.</summary>
    public static class Windowing {
        /// <summary>Apply a custom window function.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Function">The custom window function, of which the parameter is the position in the signal from 0 to 2 * pi,
        /// and its return value is the multiplier for the sample at that point</param>
        public static void ApplyWindow(float[] Samples, Func<float, float> Function) {
            for (int Sample = 0, c = Samples.Length; Sample < c; ++Sample)
                Samples[Sample] *= Function(Measurements.Pix2 * Sample / c);
        }

        /// <summary>Apply a custom window function on a complex signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Function">The custom window function, of which the parameter is the position in the signal from 0 to 2 * pi,
        /// and its return value is the multiplier for the sample at that point</param>
        public static void ApplyWindow(Complex[] Samples, Func<float, float> Function) {
            for (int Sample = 0, c = Samples.Length; Sample < c; ++Sample)
                Samples[Sample] *= Function(Measurements.Pix2 * Sample / c);
        }

        /// <summary>Apply a custom window function on part of a signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Start">Beginning of the window in samples</param>
        /// <param name="End">End of the window in samples</param>
        /// <param name="Function">The custom window function, of which the parameter is the position in the signal from 0 to 2 * pi,
        /// and its return value is the multiplier for the sample at that point</param>
        public static void ApplyWindow(float[] Samples, int Start, int End, Func<float, float> Function) {
            int Span = End - Start;
            for (int Sample = Start; Sample < End; ++Sample)
                Samples[Sample] *= Function(Measurements.Pix2 * (Sample - Start) / End);
        }

        /// <summary>Apply a custom window function on part of a complex signal.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Start">Beginning of the window in samples</param>
        /// <param name="End">End of the window in samples</param>
        /// <param name="Function">The custom window function, of which the parameter is the position in the signal from 0 to 2 * pi,
        /// and its return value is the multiplier for the sample at that point</param>
        public static void ApplyWindow(Complex[] Samples, int Start, int End, Func<float, float> Function) {
            int Span = End - Start;
            for (int Sample = Start; Sample < End; ++Sample)
                Samples[Sample] *= Function(Measurements.Pix2 * (Sample - Start) / End);
        }

        /// <summary>Apply a predefined window function.</summary>
        /// <param name="Samples">Measurement to window</param>
        /// <param name="Function">Window function</param>
        public static void ApplyWindow(float[] Samples, Window Function) {
            switch (Function) {
                case Window.Sine: ApplyWindow(Samples, SineWindow); return;
                case Window.Hamming: ApplyWindow(Samples, HammingWindow); return;
                case Window.Hann: ApplyWindow(Samples, HannWindow); return;
                case Window.Blackman: ApplyWindow(Samples, BlackmanWindow); return;
                case Window.BlackmanHarris: ApplyWindow(Samples, BlackmanHarrisWindow); return;
                case Window.Void: ApplyWindow(Samples, VoidWindow); return;
                default: break;
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
        /// <summary>A window designed to flatten sweep responses.</summary>
        static float VoidWindow(float x) {
            const float Flatness = .9f, SinMod = 2f / Flatness;
            return x < Measurements.Pix2 * Flatness ? x > Measurements.Pix2 * (1 - Flatness) ? 1 :
                Mathf.Sin(x * SinMod) : Mathf.Sin((Measurements.Pix2 - x) * SinMod);
        }
    }
}