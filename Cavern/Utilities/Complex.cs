﻿using System;

namespace Cavern.Utilities {
    /// <summary>A complex number.</summary>
    public struct Complex {
        /// <summary>Real part of the complex number.</summary>
        public float Real;
        /// <summary>Imaginary part of the complex number.</summary>
        public float Imaginary;

        /// <summary>Constructor from coordinates.</summary>
        public Complex(float real = 0, float imaginary = 0) {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>Magnitude of the complex number (spectrum for FFT).</summary>
        public float Magnitude {
            get => (float)Math.Sqrt(Real * Real + Imaginary * Imaginary);
            set => this *= value / Magnitude;
        }

        /// <summary>Direction of the complex number (phase for FFT).</summary>
        public float Phase => (float)Math.Atan(Imaginary / Real);

        /// <summary>Multiply by (cos(x), sin(x)).</summary>
        public void Rotate(float angle) {
            float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle), oldReal = Real;
            Real = Real * cos - Imaginary * sin;
            Imaginary = oldReal * sin + Imaginary * cos;
        }

        /// <summary>Complex addition.</summary>
        public static Complex operator +(Complex lhs, Complex rhs) => new Complex(lhs.Real + rhs.Real, lhs.Imaginary + rhs.Imaginary);

        /// <summary>Complex substraction.</summary>
        public static Complex operator -(Complex lhs, Complex rhs) => new Complex(lhs.Real - rhs.Real, lhs.Imaginary - rhs.Imaginary);

        /// <summary>Complex multiplication.</summary>
        public static Complex operator *(Complex lhs, Complex rhs) =>
            new Complex(lhs.Real * rhs.Real - lhs.Imaginary * rhs.Imaginary, lhs.Real * rhs.Imaginary + lhs.Imaginary * rhs.Real);

        /// <summary>Scalar complex multiplication.</summary>
        public static Complex operator *(Complex lhs, float rhs) => new Complex(lhs.Real * rhs, lhs.Imaginary * rhs);

        /// <summary>Complex division.</summary>
        public static Complex operator /(Complex lhs, Complex rhs) {
            float multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary);
            return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) * multiplier,
                (lhs.Imaginary * rhs.Real - lhs.Real * rhs.Imaginary) * multiplier);
        }

        /// <summary>Convert a float array to complex.</summary>
        public static Complex[] Parse(float[] source) {
            Complex[] result = new Complex[source.Length];
            for (int i = 0; i < source.Length; ++i)
                result[i].Real = source[i];
            return result;
        }

        /// <summary>Multiply with another complex number.</summary>
        public void Multiply(ref Complex rhs) {
            float oldReal = Real;
            Real = Real * rhs.Real - Imaginary * rhs.Imaginary;
            Imaginary = oldReal * rhs.Imaginary + Imaginary * rhs.Real;
        }

        /// <summary>Divide with another complex number.</summary>
        public void Divide(ref Complex rhs) {
            float multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary), oldReal = Real;
            Real = (Real * rhs.Real + Imaginary * rhs.Imaginary) * multiplier;
            Imaginary = (Imaginary * rhs.Real - oldReal * rhs.Imaginary) * multiplier;
        }

        /// <summary>Display the complex number.</summary>
        public override string ToString() {
            if (Imaginary >= 0)
                return string.Format("{0}+{1}i", Real, Imaginary);
            else
                return string.Format("{0}{1}i", Real, Imaginary);
        }
    }
}