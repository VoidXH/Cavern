using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Operations on complex arrays.
    /// </summary>
    public static class ComplexArray {
        /// <summary>
        /// Add the <paramref name="other"/> array's each element to the same indexes in the <paramref name="source"/>.
        /// </summary>
        public static unsafe void Add(this Complex[] source, Complex[] other) {
            fixed (Complex* pSource = source)
            fixed (Complex* pOther = other) {
                Complex* lhs = pSource,
                    rhs = pOther,
                    end = pSource + source.Length;
                while (lhs != end) {
                    *lhs++ += *rhs++;
                }
            }
        }

        /// <summary>
        /// Get the average of multiple transfer functions.
        /// </summary>
        public static Complex[] Average(this Complex[][] sources) {
            Complex[] result = new Complex[sources[0].Length];
            for (int i = 0; i < sources.Length; i++) {
                Add(result, sources[i]);
            }
            Gain(result, 1f / sources.Length);
            return result;
        }

        /// <summary>
        /// Get the average of multiple frequency responses.
        /// </summary>
        public static float[] AverageMagnitudes(this Complex[][] sources) {
            float[] result = new float[sources[0].Length >> 1];
            for (int i = 0; i < sources.Length; i++) {
                Complex[] source = sources[i];
                for (int j = 0; j < result.Length; j++) {
                    result[j] += source[j].Magnitude;
                }
            }
            WaveformUtils.Gain(result, 1f / sources.Length);
            return result;
        }

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
            for (int i = 0; i < source.Length; i++) {
                float multiplier = 1 / (other[i].Real * other[i].Real + other[i].Imaginary * other[i].Imaginary),
                    oldReal = source[i].Real;
                source[i].Real = (source[i].Real * other[i].Real + source[i].Imaginary * other[i].Imaginary) * multiplier;
                source[i].Imaginary = (source[i].Imaginary * other[i].Real - oldReal * other[i].Imaginary) * multiplier;
            }
        }

        /// <summary>
        /// Multiply all elements in the <paramref name="array"/> with the <paramref name="gain"/>.
        /// </summary>
        public static unsafe void Gain(this Complex[] array, float gain) {
            fixed (Complex* pArray = array) {
                Complex* lhs = pArray,
                    end = pArray + array.Length;
                while (lhs != end) {
                    *lhs++ *= gain;
                }
            }
        }

        /// <summary>
        /// Get the root mean square of the values' magnitudes in a <see cref="Complex"/> <paramref name="array"/>.
        /// </summary>
        public static unsafe float GetRMSMagnitude(this Complex[] array) {
            float sum = 0;
            fixed (Complex* pArray = array) {
                Complex* lhs = pArray,
                    end = pArray + array.Length;
                while (lhs != end) {
                    sum += (*lhs++).SqrMagnitude;
                }
            }
            return MathF.Sqrt(sum / array.Length);
        }

        /// <summary>
        /// Get the maximum at each position of the transfer functions.
        /// </summary>
        public static unsafe Complex[] Max(this Complex[][] sources) {
            Complex[] result = new Complex[sources[0].Length];
            fixed (Complex* pTarget = result) {
                Complex* end = pTarget + result.Length;
                for (int i = 0; i < sources.Length; i++) {
                    fixed (Complex* pSource = sources[i]) {
                        Complex* source = pSource,
                            target = pTarget;
                        while (target != end) {
                            if ((*target).SqrMagnitude < (*source).SqrMagnitude) {
                                *target = *source;
                            }
                            source++;
                            target++;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Convert a float array to complex a size that's ready for FFT.
        /// </summary>
        public static Complex[] ParseForFFT(this float[] source) {
            Complex[] result = new Complex[QMath.Base2Ceil(source.Length)];
            for (int i = 0; i < source.Length; i++) {
                result[i].Real = source[i];
            }
            return result;
        }

        /// <summary>
        /// Move the waveform to a complex array before it's Fourier-transformed.
        /// </summary>
        /// <remarks>This function clears the imaginary part, allowing the use of reusable arrays.</remarks>
        public static void ParseForFFT(this float[] source, Complex[] target) {
            for (int i = 0; i < source.Length; i++) {
                target[i] = new Complex(source[i]);
            }
        }

        /// <summary>
        /// Set this array to the FFT of the Dirac delta function, which is constant 1.
        /// </summary>
        public static void SetToDiracDelta(this Complex[] array) {
            for (int i = 0; i < array.Length; i++) {
                array[i] = new Complex(1);
            }
        }

        /// <summary>
        /// Swap the real and imaginary planes.
        /// </summary>
        public static void SwapDimensions(this Complex[] array) {
            for (int i = 0; i < array.Length; i++) {
                (array[i].Real, array[i].Imaginary) = (array[i].Imaginary, array[i].Real);
            }
        }
    }
}