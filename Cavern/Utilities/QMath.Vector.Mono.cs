namespace Cavern.Utilities {
    // Non-vectorized versions of vector functions, as they run faster in Mono.
    public static partial class QMath {
        /// <summary>
        /// Multiply the values of both arrays together and add these multiples together.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe float MultiplyAndAdd_Mono(float* lhs, float* rhs, int count) {
            float sum = 0;
            while (count-- != 0) {
                sum += *lhs++ * *rhs++;
            }
            return sum;
        }

        /// <summary>
        /// Multiply the values of both arrays together to the corresponding element of the <paramref name="target"/>.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndAdd_Mono(float* lhs, float* rhs, float* target, int count) {
            while (count-- != 0) {
                *target++ += *lhs++ * *rhs++;
            }
        }

        /// <summary>
        /// Multiply the values of an array with a constant to the corresponding element of the <paramref name="target"/>.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndAdd_Mono(float* lhs, float rhs, float* target, int count) {
            while (count-- != 0) {
                *target++ += *lhs++ * rhs;
            }
        }

        /// <summary>
        /// Do <see cref="MultiplyAndAdd_Mono(float*, float*, float*, int)"/> simultaneously for two different pairs of arrays.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndAdd_Mono(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count) {
            while (count-- != 0) {
                *target++ += *lhs1++ * *rhs1++ + *lhs2++ * *rhs2++;
            }
        }

        /// <summary>
        /// Do <see cref="MultiplyAndAdd_Mono(float*, float, float*, int)"/> simultaneously for two different arrays.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndAdd_Mono(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count) {
            while (count-- != 0) {
                *target++ += *lhs1++ * rhs1 + *lhs2++ * rhs2;
            }
        }

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd_Mono(float*, float*, float*, int)"/>.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndSet_Mono(float* lhs, float* rhs, float* target, int count) {
            while (count-- != 0) {
                *target++ = *lhs++ * *rhs++;
            }
        }

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd_Mono(float*, float*, float*, float*, float*, int)"/>.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndSet_Mono(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count) {
            while (count-- != 0) {
                *target++ = *lhs1++ * *rhs1++ + *lhs2++ * *rhs2++;
            }
        }

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd_Mono(float*, float, float*, float, float*, int)"/>.
        /// This version of the function is faster only in a Mono runtime (like Unity).
        /// </summary>
        public static unsafe void MultiplyAndSet_Mono(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count) {
            while (count-- != 0) {
                *target++ = *lhs1++ * rhs1 + *lhs2++ * rhs2;
            }
        }
    }
}