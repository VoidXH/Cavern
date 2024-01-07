using Cavern.QuickEQ.Crossover;

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
        public void ImpulseResponse() => Utils.ImpulseResponse(new BasicCrossover(null, null), 0.49152157f, 0.50847834f);
    }
}