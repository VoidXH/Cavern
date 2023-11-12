using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// A complex number.
    /// </summary>
    public struct Complex : IComparable<float>, IComparable<Complex>, IEquatable<float>, IEquatable<Complex> {
        /// <summary>
        /// Real part of the complex number.
        /// </summary>
        public float Real { get; set; }

        /// <summary>
        /// Imaginary part of the complex number.
        /// </summary>
        public float Imaginary { get; set; }

        /// <summary>
        /// Magnitude of the complex number (spectrum for FFT).
        /// </summary>
        public float Magnitude {
            get => MathF.Sqrt(Real * Real + Imaginary * Imaginary);
            set => this *= value / Magnitude;
        }

        /// <summary>
        /// Direction of the complex number (phase for FFT).
        /// </summary>
        public float Phase => MathF.Atan2(Imaginary, Real);

        /// <summary>
        /// Squared magnitude of the complex number.
        /// </summary>
        public float SqrMagnitude => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Complex number from a scalar.
        /// </summary>
        public Complex(float real) {
            Real = real;
            Imaginary = 0;
        }

        /// <summary>
        /// Complex number from coordinates.
        /// </summary>
        public Complex(float real, float imaginary) {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Complex addition.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex lhs, Complex rhs) =>
            new Complex(lhs.Real + rhs.Real, lhs.Imaginary + rhs.Imaginary);

        /// <summary>
        /// Complex subtraction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex lhs, Complex rhs) =>
            new Complex(lhs.Real - rhs.Real, lhs.Imaginary - rhs.Imaginary);

        /// <summary>
        /// Complex negation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex pon) => new Complex(-pon.Real, -pon.Imaginary);

        /// <summary>
        /// Complex multiplication.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex lhs, Complex rhs) =>
            new Complex(lhs.Real * rhs.Real - lhs.Imaginary * rhs.Imaginary,
                lhs.Real * rhs.Imaginary + lhs.Imaginary * rhs.Real);

        /// <summary>
        /// Scalar complex multiplication.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex lhs, float rhs) => new Complex(lhs.Real * rhs, lhs.Imaginary * rhs);

        /// <summary>
        /// Complex division.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator /(Complex lhs, Complex rhs) {
            float multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary);
            return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) * multiplier,
                (lhs.Imaginary * rhs.Real - lhs.Real * rhs.Imaginary) * multiplier);
        }

        /// <summary>
        /// Convert a float array to complex.
        /// </summary>
        public static Complex[] Parse(float[] source) {
            Complex[] result = new Complex[source.Length];
            for (int i = 0; i < source.Length; ++i) {
                result[i].Real = source[i];
            }
            return result;
        }

        /// <summary>
        /// Gets the complex number with a <see cref="Magnitude"/> of 1 and a desired <see cref="Phase"/>.
        /// </summary>
        public static Complex UnitPhase(float phase) => new Complex(MathF.Cos(phase), MathF.Sin(phase));

        /// <summary>
        /// True if the number is 0 + 0i.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZero() => Real == 0 && Imaginary == 0;

        /// <summary>
        /// Get the complex logarithm of a real number.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Log(float x) => new Complex(MathF.Log(Math.Abs(x)), x >= 0 ? 0 : 1.36437635f);

        /// <summary>
        /// Zero this number the fastest.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Real = 0;
            Imaginary = 0;
        }

        /// <summary>
        /// Multiply with another complex number.
        /// </summary>
        public void Multiply(Complex rhs) {
            float oldReal = Real;
            Real = Real * rhs.Real - Imaginary * rhs.Imaginary;
            Imaginary = oldReal * rhs.Imaginary + Imaginary * rhs.Real;
        }

        /// <summary>
        /// Divide with another complex number.
        /// </summary>
        public void Divide(Complex rhs) {
            float multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary),
                oldReal = Real;
            Real = (Real * rhs.Real + Imaginary * rhs.Imaginary) * multiplier;
            Imaginary = (Imaginary * rhs.Real - oldReal * rhs.Imaginary) * multiplier;
        }

        /// <summary>
        /// Calculate 1 / z.
        /// </summary>
        public Complex Invert() {
            float mul = 1 / (Real * Real + Imaginary * Imaginary);
            return new Complex(Real * mul, Imaginary * mul);
        }

        /// <summary>
        /// Multiply by (cos(x), sin(x)).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(float angle) {
            float cos = MathF.Cos(angle), sin = MathF.Sin(angle), oldReal = Real;
            Real = Real * cos - Imaginary * sin;
            Imaginary = oldReal * sin + Imaginary * cos;
        }

        /// <summary>
        /// Compare thie number to an <paramref name="other"/> if it precedes, follows, or matches it in a sort.
        /// </summary>
        public int CompareTo(float other) => Magnitude.CompareTo(other);

        /// <summary>
        /// Compare thie number to an <paramref name="other"/> if it precedes, follows, or matches it in a sort.
        /// </summary>
        public int CompareTo(Complex other) => Magnitude.CompareTo(other.Magnitude);

        /// <summary>
        /// Check if this number equals an <paramref name="other"/>.
        /// </summary>
        public bool Equals(float other) => Real == other && Imaginary == 0;

        /// <summary>
        /// Check if this number equals an <paramref name="other"/>.
        /// </summary>
        public bool Equals(Complex other) => Real == other.Real && Imaginary == other.Imaginary;

        /// <summary>
        /// Display the complex number.
        /// </summary>
        public override string ToString() => string.Format(Imaginary >= 0 ? "{0}+{1}i" : "{0}{1}i", Real, Imaginary);
    }
}