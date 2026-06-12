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
            Console.WriteLine("Running without CavernAmp...");
            test();
            CavernAmp.Bypass = false;
            if (CavernAmp.Available) {
                Console.WriteLine("Running with CavernAmp...");
                test();
            } else {
                Console.WriteLine("CavernAmp is not available, but the managed version's test ran successfully.");
            }
        }

        /// <summary>
        /// Run a test that can use <see cref="CavernAmp"/> with and without <see cref="CavernAmp"/>.
        /// </summary>
        public static void RunPlusMono(Action test) {
            CavernAmp.Bypass = true;

            CavernAmp.MonoOverride = false;
            Console.WriteLine("Running with managed Cavern (fast)...");
            test();

            CavernAmp.MonoOverride = true;
            Console.WriteLine("Running with managed Cavern (Mono)...");
            test();

            CavernAmp.MonoOverride = null;
            CavernAmp.Bypass = false;
            if (CavernAmp.Available) {
                Console.WriteLine("Running with CavernAmp...");
                test();
            } else {
                Console.WriteLine("CavernAmp is not available, but the managed version's test ran successfully.");
            }
        }
    }
}
