using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class PeakingEqualizer {
        /// <summary>
        /// Parse a file created in the standard PEQ filter list format.
        /// </summary>
        public static PeakingEQ[] ParseEQFile(string path) => ParseEQFile(File.ReadLines(path));

        /// <summary>
        /// Parse a file created in the standard PEQ filter list format.
        /// </summary>
        public static PeakingEQ[] ParseEQFile(IEnumerable<string> lines) {
            List<PeakingEQ> result = new List<PeakingEQ>();
            foreach (string line in lines) {
                string[] parts = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 11 && parts[0].ToLowerInvariant() == "filter" && parts[2].ToLowerInvariant() == "on" &&
                    parts[3].ToLowerInvariant() == "pk" && QMath.TryParseDouble(parts[5], out double freq) &&
                    QMath.TryParseDouble(parts[8], out double gain) && QMath.TryParseDouble(parts[11], out double q)) {
                    result.Add(new PeakingEQ(Listener.DefaultSampleRate, freq, q, gain));
                }
            }
            return result.ToArray();
        }
    }
}
