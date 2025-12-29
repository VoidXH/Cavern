using Cavern.QuickEQ.Crossover;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tests the <see cref="CavernCrossover"/> class.
    /// </summary>
    [TestClass]
    public class CavernCrossover_Tests {
        /// <summary>
        /// Tests if <see cref="CavernCrossover"/> generates correct impulse responses.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ImpulseResponse() => Utils.ImpulseResponse(new CavernCrossover(null), 0.3567448f, 0.7216354f);
    }
}