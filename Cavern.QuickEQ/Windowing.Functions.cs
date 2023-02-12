using System;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Available FFT windowing functions.
    /// </summary>
    public enum Window {
        /// <summary>
        /// No windowing.
        /// </summary>
        Disabled,
        /// <summary>
        /// 1
        /// </summary>
        Rectangular,
        /// <summary>
        /// sin(x)
        /// </summary>
        Sine,
        /// <summary>
        /// 0.54 - 0.46 * cos(x)
        /// </summary>
        Hamming,
        /// <summary>
        /// 0.5 * (1 - cos(x))
        /// </summary>
        Hann,
        /// <summary>
        /// 0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)
        /// </summary>
        Blackman,
        /// <summary>
        /// 0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)
        /// </summary>
        BlackmanHarris,
        /// <summary>
        /// A window for impulse response trimming, with a precompiled alpha.
        /// </summary>
        Tukey
    }

    public static partial class Windowing {
        /// <summary>
        /// Window function format.
        /// </summary>
        /// <param name="x">The position in the signal from 0 to 2 * pi</param>
        /// <returns>The multiplier for the sample at x</returns>
        delegate float WindowFunction(float x);

        /// <summary>
        /// Window function format in double precision.
        /// </summary>
        /// <param name="x">The position in the signal from 0 to 2 * pi</param>
        /// <returns>The multiplier for the sample at x</returns>
        delegate double WindowFunctionDouble(double x);

        /// <summary>
        /// Get the corresponding window function for each <see cref="Window"/> value.
        /// </summary>
        static WindowFunction GetWindowFunction(Window function) => function switch {
            Window.Sine => SineWindow,
            Window.Hamming => HammingWindow,
            Window.Hann => HannWindow,
            Window.Blackman => BlackmanWindow,
            Window.BlackmanHarris => BlackmanHarrisWindow,
            Window.Tukey => TukeyWindow,
            _ => x => 1,
        };

        /// <summary>
        /// Get the corresponding double precision window function for each <see cref="Window"/> value.
        /// </summary>
        static WindowFunctionDouble GetWindowFunctionDouble(Window function) => function switch {
            Window.Sine => SineWindow,
            Window.Hamming => HammingWindow,
            Window.Hann => HannWindow,
            Window.Blackman => BlackmanWindow,
            Window.BlackmanHarris => BlackmanHarrisWindow,
            Window.Tukey => TukeyWindow,
            _ => x => 1,
        };

        /// <summary>
        /// sin(x)
        /// </summary>
        static float SineWindow(float x) => MathF.Sin(x * .5f);

        /// <summary>
        /// sin(x)
        /// </summary>
        static double SineWindow(double x) => Math.Sin(x * .5);

        /// <summary>
        /// 0.54 - 0.46 * cos(x)
        /// </summary>
        static float HammingWindow(float x) => .54f - .46f * MathF.Cos(x);

        /// <summary>
        /// 0.54 - 0.46 * cos(x)
        /// </summary>
        static double HammingWindow(double x) => .54 - .46 * Math.Cos(x);

        /// <summary>
        /// 0.5 * (1 - cos(x))
        /// </summary>
        static float HannWindow(float x) => .5f * (1 - MathF.Cos(x));

        /// <summary>
        /// 0.5 * (1 - cos(x))
        /// </summary>
        static double HannWindow(double x) => .5 * (1 - Math.Cos(x));

        /// <summary>
        /// 0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)
        /// </summary>
        static float BlackmanWindow(float x) => .42f - .5f * MathF.Cos(x) + .08f * MathF.Cos(x + x);

        /// <summary>
        /// 0.42 - 0.5 * cos(x) + 0.08 * cos(2 * x)
        /// </summary>
        static double BlackmanWindow(double x) => .42 - .5 * Math.Cos(x) + .08 * Math.Cos(x + x);

        /// <summary>
        /// 0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)
        /// </summary>
        static float BlackmanHarrisWindow(float x) {
            float x2 = x + x;
            return .35875f - .48829f * MathF.Cos(x) + .14128f * MathF.Cos(x2) - .01168f * MathF.Cos(x2 + x);
        }

        /// <summary>
        /// 0.35875 - 0.48829 * cos(x) + 0.14128 * cos(2 * x) - 0.01168 * cos(3 * x)
        /// </summary>
        static double BlackmanHarrisWindow(double x) {
            double x2 = x + x;
            return .35875 - .48829 * Math.Cos(x) + .14128 * Math.Cos(x2) - .01168 * Math.Cos(x2 + x);
        }

        /// <summary>
        /// A window for impulse response trimming, with a precompiled alpha.
        /// </summary>
        static float TukeyWindow(float x) {
            const float alpha = .25f,
                positioner = 1 / alpha;
            if (x < MathF.PI * alpha) {
                return (MathF.Cos(x * positioner - MathF.PI) + 1) * .5f;
            } else if (x > MathF.PI * (2 - alpha)) {
                return (MathF.Cos((2 * MathF.PI - x) * positioner - MathF.PI) + 1) * .5f;
            } else {
                return 1;
            }
        }

        /// <summary>
        /// A window for impulse response trimming, with a precompiled alpha.
        /// </summary>
        static double TukeyWindow(double x) {
            const double alpha = .25,
                positioner = 1 / alpha;
            if (x < Math.PI * alpha) {
                return (Math.Cos(x * positioner - Math.PI) + 1) * .5;
            } else if (x > Math.PI * (2 - alpha)) {
                return (Math.Cos((2 * Math.PI - x) * positioner - Math.PI) + 1) * .5;
            } else {
                return 1;
            }
        }
    }
}