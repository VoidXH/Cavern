using System.Diagnostics;

namespace Cavern.Utilities {
    /// <summary>Useful functions used in multiple classes.</summary>
    public static class Utils {
        /// <summary>Cached version name.</summary>
        static string info;
        /// <summary>Version and creator information.</summary>
        public static string Info => info ?? (info = "Cavern v" +
            FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + " by VoidX (www.cavern.cf)");
    }
}