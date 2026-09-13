using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    public static partial class ComplexArray {
        /// <summary>
        /// Add the <paramref name="other"/> array's each element to the same indexes in the <paramref name="source"/>.
        /// </summary>
        public static unsafe void Add(this Complex[] source, Complex[] other) {
            source.ThrowIfNullOrEmpty(nameof(source));
            other.ThrowIfNullOrEmpty(nameof(other));
            if (source.Length != other.Length) {
                throw new ArgumentException("Arrays must be of the same length to be added together.");
            }

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
        /// Get the maximum at each position of the transfer functions.
        /// </summary>
        public static unsafe Complex[] Max(this Complex[][] sources) {
            sources.ThrowIfNullOrEmpty(nameof(sources));
            Complex[] result = new Complex[sources[0].Length];
            fixed (Complex* pTarget = result) {
                Complex* end = pTarget + result.Length;
                for (int i = 0; i < sources.Length; i++) {
                    sources[i].ThrowIfNullOrEmpty(nameof(sources));
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
        /// Calculate the prefix sums (cumulative sums) of the directions (normalized complex values) of a complex array.
        /// The returned array has a length of <paramref name="array"/>.Length + 1 with result[0] = 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex[] PrefixSumDirections(this Complex[] array) => PrefixSumDirections(array, array.Length);

        /// <summary>
        /// Calculate the prefix sums (cumulative sums) of the directions (normalized complex values) of a complex array until the selected border element (exclusive).
        /// The returned array has a length of <paramref name="until"/> + 1 with result[0] = 0.
        /// </summary>
        public static Complex[] PrefixSumDirections(this Complex[] array, int until) {
            Complex[] result = new Complex[until + 1];
            for (int i = 0; i < until; i++) {
                float mag = array[i].Magnitude;
                if (mag > 0) {
                    result[i + 1] = result[i] + array[i] * (1 / mag);
                } else {
                    result[i + 1] = result[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Calculate the prefix sums (cumulative sums) of the magnitudes of a complex array.
        /// The returned array has a length of <paramref name="array"/>.Length + 1 with result[0] = 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] PrefixSumMagnitudes(this Complex[] array) => PrefixSumMagnitudes(array, array.Length);

        /// <summary>
        /// Calculate the prefix sums (cumulative sums) of the magnitudes of a complex array until the selected border element (exclusive).
        /// The returned array has a length of <paramref name="until"/> + 1 with result[0] = 0.
        /// </summary>
        public static float[] PrefixSumMagnitudes(this Complex[] array, int until) {
            float[] result = new float[until + 1];
            for (int i = 0; i < until; i++) {
                result[i + 1] = result[i] + array[i].Magnitude;
            }
            return result;
        }
    }
}
