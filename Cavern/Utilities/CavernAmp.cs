using System.Runtime.InteropServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Drastically faster versions of some functions written in C++.
    /// </summary>
    public static class CavernAmp {
        static bool available;
        static bool tested = false;

        /// <summary>
        /// Is the CavernAmp DLL present and the platform is correct?
        /// </summary>
        public static bool Available {
            get {
                if (tested)
                    return available;
                try {
                    available = IsAvailable();
                } catch {
                    available = false;
                }
                tested = true;
                return available;
            }
        }

        [DllImport("CavernAmp.dll", EntryPoint = "IsAvailable")]
        static extern bool IsAvailable();
    }
}