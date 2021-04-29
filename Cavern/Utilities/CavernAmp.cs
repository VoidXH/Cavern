using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    /// <summary>Drastically faster versions of some functions written in C++.</summary>
    public static class CavernAmp {
        /// <summary>Is the CavernAmp DLL present and the platform is correct?</summary>
        static bool available;
        /// <summary>Force disable CavernAmp for performance benchmarks.</summary>
        static bool bypass = false;
        /// <summary>True if CavernAmp DLL was checked if <see cref="available"/>.</summary>
        static bool tested = false;

        /// <summary>Is the CavernAmp DLL present and the platform is correct?</summary>
        public static bool Available {
            get {
                if (tested)
                    return available;

                if (bypass)
                    return available = false;
                try {
                    available = IsAvailable();
                } catch {
                    available = false;
                }
                tested = true;
                return available;
            }
        }

        /// <summary>Force disable CavernAmp for performance benchmarks.</summary>
        public static bool Bypass {
            set {
                bypass = value;
                if (available)
                    available = false;
                if (!bypass)
                    tested = false;
            }
        }

        /// <summary>When the DLL is present near the executable and the platform matches, this returns true.</summary>
        [DllImport("CavernAmp.dll", EntryPoint = "IsAvailable")]
        static extern bool IsAvailable();
    }
}