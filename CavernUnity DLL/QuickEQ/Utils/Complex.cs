using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>A complex number.</summary>
    public class Complex {
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
        public float Magnitude {
            get { return Mathf.Sqrt(Real * Real + Imaginary * Imaginary); }
        }

        /// <summary>Direction of the complex number (phase for FFT).</summary>
        public float Phase {
            get { return Mathf.Atan(Imaginary / Real); }
        }

        /// <summary>Complex addition.</summary>
        public static Complex operator +(Complex a, Complex b) {
            return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        /// <summary>Complex substraction.</summary>
        public static Complex operator -(Complex a, Complex b) {
            return new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
        }

        /// <summary>Complex multiplication.</summary>
        public static Complex operator *(Complex a, Complex b) {
            return new Complex(a.Real * b.Real - a.Imaginary * b.Imaginary, a.Real * b.Imaginary + a.Imaginary * b.Real);
        }

        /// <summary>Complex division.</summary>
        public static Complex operator /(Complex a, Complex b) {
            float Divisor = b.Real * b.Real + b.Imaginary * b.Imaginary;
            return Divisor != 0 ?
                new Complex((a.Real * b.Real + a.Imaginary * b.Imaginary) / Divisor, (a.Imaginary * b.Real - a.Real * b.Imaginary) / Divisor) :
                new Complex();
        }
    }
}