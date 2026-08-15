using System.Collections.Generic;

namespace Cavern.Remapping {
    /// <summary>
    /// Calculate metrics of <see cref="Upmixer"/> instances.
    /// </summary>
    public static class UpmixerAnalyzer {
        /// <summary>
        /// Get a mapping of which original <paramref name="originalSources"/> is mapped to which upmixed <paramref name="upmixedSources"/>.
        /// When multiple <paramref name="upmixedSources"/> are present at the same location, the first match will be returned.
        /// The result can be empty.
        /// </summary>
        public static Dictionary<Source, Source> GetKeptSources(IEnumerable<Source> originalSources, IEnumerable<Source> upmixedSources) {
            Dictionary<Source, Source> result = new Dictionary<Source, Source>();
            foreach (Source original in originalSources) {
                foreach (Source upmixed in upmixedSources) {
                    if (original.Position == upmixed.Position && original.LFE == upmixed.LFE) {
                        result[original] = upmixed;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
