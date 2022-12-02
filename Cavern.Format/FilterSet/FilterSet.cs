using System;

namespace Cavern.Format.FilterSet {
    public abstract class FilterSet {
        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public abstract void Export(string path);

        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, int channels, int sampleRate) {
            return device switch {
                FilterSetTarget.EqualizerAPO => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException(),
            };
        }
    }

    /// <summary>
    /// Supported software/hardware to export filters to.
    /// </summary>
    /// <remarks>Targets that need multiple passes (like MultEQ-X with its measure, load, measure, save process)
    /// are not included as a single measurement can't be exported to them.</remarks>
    public enum FilterSetTarget {
        /// <summary>
        /// Equalizer APO for Windows.
        /// </summary>
        EqualizerAPO,
        /// <summary>
        /// CamillaDSP for Windows/Mac/Linux.
        /// </summary>
        CamillaDSP,
    }
}