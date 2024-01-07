using Cavern.QuickEQ.Crossover;

namespace Test.Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tests the <see cref="SyntheticBiquadCrossover"/> class.
    /// </summary>
    [TestClass]
    public class SyntheticBiquadCrossover_Tests {
        /// <summary>
        /// Tests if <see cref="SyntheticBiquadCrossover"/> generates correct impulse responses.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ImpulseResponse() => Utils.ImpulseResponse(new SyntheticBiquadCrossover(null, null), 0.47490564f, 0.5231629f);
    }
}