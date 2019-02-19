using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>A complex number.</summary>
    public struct Complex {
        /// <summary>Real part of the complex number.</summary>
        public float Real;
        /// <summary>Imaginary part of the complex number.</summary>
        public float Imaginary;

        /// <summary>Constructor from coordinates.</summary>
        public Complex(float Real = 0, float Imaginary = 0) {
            this.Real = Real;
            this.Imaginary = Imaginary;
        }

        /// <summary>Magnitude of the complex number (spectrum for FFT).</summary>
        public float Magnitude => Mathf.Sqrt(Real * Real + Imaginary * Imaginary);

        /// <summary>Direction of the complex number (phase for FFT).</summary>
        public float Phase => Mathf.Atan(Imaginary / Real);

        /// <summary>Multiply by (cos(x), sin(x)).</summary>
        public void Rotate(float Angle) {
            float Cos = Mathf.Cos(Angle), Sin = Mathf.Sin(Angle), OldReal = Real;
            Real = Real * Cos - Imaginary * Sin;
            Imaginary = OldReal * Sin + Imaginary * Cos;
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
            float Multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary);
            return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) * Multiplier,
                (lhs.Imaginary * rhs.Real - lhs.Real * rhs.Imaginary) * Multiplier);
        }

        /// <summary>Divide with another complex number.</summary>
        public void Divide(ref Complex rhs) {
            float Multiplier = 1 / (rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary), OldReal = Real;
            Real = (Real * rhs.Real + Imaginary * rhs.Imaginary) * Multiplier;
            Imaginary = (Imaginary * rhs.Real - OldReal * rhs.Imaginary) * Multiplier;
        }
    }
}