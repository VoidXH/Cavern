using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Operations on complex arrays.
    /// </summary>
    public static class ComplexArray {
        /// <summary>
        /// Convert all elements in the <paramref name="source"/> to their conjugates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Conjugate(this Complex[] source) {
            fixed (Complex* pSource = source) {
                Complex* src = pSource,
                    end = src + source.Length;
                while (src != end) {
                    src->Imaginary = -src->Imaginary;
                    src++;
                }
            }
        }

        /// <summary>
        /// Replace the <paramref name="source"/> with its convolution with an <paramref name="other"/> array.
        /// </summary>
        public static unsafe void Convolve(this Complex[] source, Complex[] other) {
            fixed (Complex* pSource = source)
            fixed (Complex* pOther = other) {
                Complex* lhs = pSource,
                    rhs = pOther,
                    end = pSource + source.Length;
                while (lhs != end) {
                    float oldReal = lhs->Real;
                    lhs->Real = lhs->Real * rhs->Real - lhs->Imaginary * rhs->Imaginary;
                    lhs->Imaginary = oldReal * rhs->Imaginary + lhs->Imaginary * rhs->Real;
                    lhs++;
                    rhs++;
                }
            }
        }

        /// <summary>
        /// Replace the <paramref name="source"/> with its deconvolution with an <paramref name="other"/> array.
        /// </summary>
        public static void Deconvolve(this Complex[] source, Complex[] other) {
            for (int i = 0; i < source.Length; ++i) {
                float multiplier = 1 / (other[i].Real * other[i].Real + other[i].Imaginary * other[i].Imaginary),
                    oldReal = source[i].Real;
                source[i].Real = (source[i].Real * other[i].Real + source[i].Imaginary * other[i].Imaginary) * multiplier;
                source[i].Imaginary = (source[i].Imaginary * other[i].Real - oldReal * other[i].Imaginary) * multiplier;
            }
        }

        /// <summary>
        /// Convert a float array to complex a size that's ready for FFT.
        /// </summary>
        public static Complex[] ParseForFFT(this float[] source) {
            Complex[] result = new Complex[QMath.Base2Ceil(source.Length)];
            for (int i = 0; i < source.Length; ++i) {
                result[i].Real = source[i];
            }
            return result;
        }

        /// <summary>
        /// Move the waveform to a complex array before it's Fourier-transformed.
        /// </summary>
        /// <remarks>This function clears the imaginary part, allowing the use of reusable arrays.</remarks>
        public static void ParseForFFT(this float[] source, Complex[] target) {
            for (int i = 0; i < source.Length; ++i) {
                target[i] = new Complex(source[i]);
            }
        }
    }
}