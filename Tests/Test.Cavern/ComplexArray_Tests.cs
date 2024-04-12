using Cavern.Utilities;

using Test.Cavern.Filters;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="ComplexArray"/> class.
    /// </summary>
    [TestClass]
    public class ComplexArray_Tests {
        /// <summary>
        /// Tests the <see cref="ComplexArray.Convolve(Complex[], Complex[])"/> method.
        /// </summary>
        /// <remarks>Further convolution tests are under <see cref="Convolver_Tests"/>.</remarks>
        [TestMethod, Timeout(1000)]
        public void Convolve() {
            const int length = 32;
            Complex[] lhs = Generators.DiracFourier(length),
                rhs = Generators.Sine(length).ParseForFFT();
            lhs.Convolve(rhs);
            CollectionAssert.AreEqual(lhs, rhs);

            lhs.SetToDiracDelta();
            rhs.SwapDimensions();
            lhs.Convolve(rhs);
            CollectionAssert.AreEqual(lhs, rhs);
        }
    }
}