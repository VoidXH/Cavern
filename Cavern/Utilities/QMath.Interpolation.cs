using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    partial class QMath {
        /// <summary>
        /// Convert an FFT bin index to the corresponding frequency in Hz.
        /// </summary>
        /// <param name="bin">FFT bin index.</param>
        /// <param name="sampleRate">Sample rate of the signal.</param>
        /// <param name="fftSize">Size of the FFT.</param>
        /// <returns>The frequency in Hz corresponding to the given bin index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double BinToFrequency(int bin, int sampleRate, int fftSize) => bin * sampleRate / (double)fftSize;

        /// <summary>
        /// Convert a frequency in Hz to the corresponding FFT bin index.
        /// </summary>
        /// <param name="frequency">Frequency in Hz.</param>
        /// <param name="sampleRate">Sample rate of the signal.</param>
        /// <param name="fftSize">Size of the FFT.</param>
        /// <returns>The FFT bin index corresponding to the given frequency.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FrequencyToBin(double frequency, int sampleRate, int fftSize) => (int)(fftSize * frequency / sampleRate);

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float from, float to, float t) => (to - from) * t + from;

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double from, double to, double t) => (to - from) * t + from;

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 from, Vector2 to, float t) => (to - from) * t + from;

        /// <summary>
        /// Unclamped linear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 from, Vector3 to, float t) => (to - from) * t + from;

        /// <summary>
        /// Gets the interpolant (t) of a linear interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpInverse(float from, float to, float value) => (value - from) / (to - from);

        /// <summary>
        /// Gets the interpolant (t) of a linear interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LerpInverse(double from, double to, double value) => (value - from) / (to - from);

        /// <summary>
        /// Gets the interpolant (t) of a linear interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpInverse(Complex from, Complex to, Complex value) {
            float fromPhase = from.Phase;
            return (value.Phase - fromPhase) / (to.Phase - fromPhase);
        }

        /// <summary>
        /// Unclamped logarithmic interpolation.
        /// </summary>
        public static double Lorp(double from, double to, double t) {
            if (from <= 0 || to <= 0) {
                throw new ArgumentException("Logarithmic interpolation requires positive values.");
            }

            double eps = 1e-10;
            if (from < eps) {
                from = eps;
            }
            if (to < eps) {
                to = eps;
            }

            return Math.Exp(Lerp(Math.Log(from), Math.Log(to), t));
        }

        /// <summary>
        /// Gets the interpolant (t) of a logarithmic interpolation for a given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LorpInverse(double from, double to, double value) {
            if (from == 0) {
                from = 1e-10; // Allow logarithmic interpolation from zero
            }
            return LerpInverse(Math.Log(from), Math.Log(to), Math.Log(value));
        }
    }
}
