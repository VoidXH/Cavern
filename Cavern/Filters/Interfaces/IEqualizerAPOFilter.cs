using System.Collections.Generic;

namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// This filter can be exported to Equalizer APO.
    /// </summary>
    public interface IEqualizerAPOFilter {
        /// <summary>
        /// Attach this filter to an Equalizer APO configuration file in the making.
        /// </summary>
        /// <param name="wipConfig">New lines that are needed in the configuration file to perform this filter
        /// will be added to this list</param>
        public abstract void ExportToEqualizerAPO(List<string> wipConfig);
    }
}