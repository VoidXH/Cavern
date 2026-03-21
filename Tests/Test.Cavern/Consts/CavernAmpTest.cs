using Cavern.Utilities;

namespace Test.Cavern.Consts {
    /// <summary>
    /// Runs tests that can use <see cref="CavernAmp"/> with and without <see cref="CavernAmp"/>.
    /// </summary>
    public static class CavernAmpTest {
        /// <summary>
        /// Run a test that can use <see cref="CavernAmp"/> with and without <see cref="CavernAmp"/>.
        /// </summary>
        public static void Run(Action test) {
            CavernAmp.Bypass = true;
            test();
            CavernAmp.Bypass = false;
            if (CavernAmp.Available) {
                test();
            }
        }
    }
}
