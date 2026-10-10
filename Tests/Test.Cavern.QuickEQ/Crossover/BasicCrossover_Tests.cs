using System.Linq;

using Cavern.Filters;
using Cavern.QuickEQ.Crossover;
using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tests the <see cref="BasicCrossover"/> class.
    /// </summary>
    [TestClass]
    public class BasicCrossover_Tests {
        /// <summary>
        /// Tests if <see cref="BasicCrossover"/> generates correct impulse responses.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ImpulseResponse() => Utils.ImpulseResponse(Crossovers.Basic5_1, .701086f, .71307665f);

        /// <summary>
        /// Tests if a 4th-order <see cref="BasicCrossover"/> creates 2 lowpass filters per channel
        /// in <see cref="BasicCrossover.GetLowpassOptimized"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetLowpassOptimized_4thOrder_Creates2LowpassFilters() {
            BasicCrossover crossover = new(Crossovers.Description5_1, 4);
            Filter result = crossover.GetLowpassOptimized(48000, 1000, 256);

            Assert.IsInstanceOfType(result, typeof(ComplexFilter));
            ComplexFilter cascade = (ComplexFilter)result;
            Assert.AreEqual(2, cascade.Filters.Count);
            Assert.IsTrue(cascade.Filters.All(x => x is Lowpass));
        }

        /// <summary>
        /// Tests if a 4th-order <see cref="BasicCrossover"/> creates 2 highpass filters per channel
        /// in <see cref="BasicCrossover.GetHighpassOptimized"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetHighpassOptimized_4thOrder_Creates2HighpassFilters() {
            BasicCrossover crossover = new(Crossovers.Description5_1, 4);
            Filter result = crossover.GetHighpassOptimized(48000, 1000, 256);

            Assert.IsInstanceOfType(result, typeof(ComplexFilter));
            ComplexFilter cascade = (ComplexFilter)result;
            Assert.AreEqual(2, cascade.Filters.Count);
            Assert.IsTrue(cascade.Filters.All(x => x is Highpass));
        }

        /// <summary>
        /// Tests if a 2nd-order <see cref="BasicCrossover"/> creates a single lowpass filter
        /// (not a <see cref="ComplexFilter"/>) in <see cref="BasicCrossover.GetLowpassOptimized"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetLowpassOptimized_2ndOrder_CreatesSingleLowpassFilter() {
            BasicCrossover crossover = new(Crossovers.Description5_1, 2);
            Filter result = crossover.GetLowpassOptimized(48000, 1000, 256);

            Assert.IsInstanceOfType(result, typeof(Lowpass));
        }
    }
}