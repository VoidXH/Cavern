using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    // Native versions of QMath functions.
    public static partial class CavernAmp {
        /// <summary>
        /// Multiply the values of both arrays together and add these multiples together.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndAdd_Sum")]
        public static extern unsafe float MultiplyAndAdd(float* lhs, float* rhs, int count);

        /// <summary>
        /// Multiply the values of both arrays together to the corresponding element of the <paramref name="target"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndAdd_PPP")]
        public static extern unsafe void MultiplyAndAdd(float* lhs, float* rhs, float* target, int count);

        /// <summary>
        /// Multiply the values of an array with a constant to the corresponding element of the <paramref name="target"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndAdd_PFP")]
        public static extern unsafe void MultiplyAndAdd(float* lhs, float rhs, float* target, int count);

        /// <summary>
        /// Do <see cref="MultiplyAndAdd(float*, float*, float*, int)"/> simultaneously for two different pairs of arrays.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndAdd_PPPPP")]
        public static extern unsafe void MultiplyAndAdd(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);

        /// <summary>
        /// Do <see cref="MultiplyAndAdd(float*, float, float*, int)"/> simultaneously for two different arrays.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndAdd_PFPFP")]
        public static extern unsafe void MultiplyAndAdd(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd(float*, float*, float*, int)"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndSet_PPP")]
        public static extern unsafe void MultiplyAndSet(float* lhs, float* rhs, float* target, int count);

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd(float*, float*, float*, float*, float*, int)"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndSet_PPPPP")]
        public static extern unsafe void MultiplyAndSet(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);

        /// <summary>
        /// Clear the <paramref name="target"/>, then do <see cref="MultiplyAndAdd(float*, float, float*, float, float*, int)"/>.
        /// </summary>
        [DllImport("CavernAmp.dll", EntryPoint = "MultiplyAndSet_PFPFP")]
        public static extern unsafe void MultiplyAndSet(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);
    }
}