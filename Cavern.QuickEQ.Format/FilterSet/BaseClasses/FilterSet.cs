using System;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set containing equalization info for each channel of a system.
    /// </summary>
    public abstract class FilterSet {
        /// <summary>
        /// Sample rate of the filter set.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Extension of the root file or the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public virtual string FileExtension => "txt";

        /// <summary>
        /// A filter set containing equalization info for each channel of a system on a given sample rate.
        /// </summary>
        public FilterSet(int sampleRate) => SampleRate = sampleRate;

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public abstract void Export(string path);

        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, int channels, int sampleRate) {
            return device switch {
                FilterSetTarget.Generic => new GenericFilterSet(channels, sampleRate),
                FilterSetTarget.GenericEqualizer => new EqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_IIR => new EqualizerAPOIIRFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLive => new DiracFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                FilterSetTarget.MiniDSP2x4Advanced => new MiniDSP2x4FilterSet(channels),
                FilterSetTarget.MiniDSP2x4HD => new MiniDSP2x4HDFilterSet(channels),
                FilterSetTarget.MultEQX => new MultEQXFilterSet(channels, sampleRate),
                FilterSetTarget.Emotiva => new EmotivaFilterSet(channels, sampleRate),
                FilterSetTarget.StormAudio => new StormAudioFilterSet(channels, sampleRate),
                FilterSetTarget.BehringerNX => new BehringerNXFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected virtual string GetLabel(int channel) => "CH" + (channel + 1);
    }

    /// <summary>
    /// Supported software/hardware to export filters to.
    /// </summary>
    /// <remarks>Targets that need multiple passes (like MultEQ-X with its measure, load, measure, save process)
    /// are not included as a single measurement can't be exported to them.</remarks>
    public enum FilterSetTarget {
        /// <summary>
        /// IIR filter sets in a commonly accepted format for maximum compatibility.
        /// </summary>
        Generic,
        /// <summary>
        /// Equalization curve sets in a commonly accepted format for maximum compatibility.
        /// </summary>
        GenericEqualizer,

        // -------------------------------------------------------------------------
        // PC targets --------------------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Equalizer APO for Windows using convolution filters.
        /// </summary>
        EqualizerAPO_FIR,
        /// <summary>
        /// Equalizer APO for Windows using peaking EQs.
        /// </summary>
        EqualizerAPO_IIR,
        /// <summary>
        /// CamillaDSP for Windows/Mac/Linux.
        /// </summary>
        CamillaDSP,

        // -------------------------------------------------------------------------
        // External DSP hardware ---------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// MiniDSP 2x4 Advanced plugin for the standard MiniDSP 2x4.
        /// </summary>
        MiniDSP2x4Advanced,
        /// <summary>
        /// MiniDSP 2x4 HD hardware DSP.
        /// </summary>
        MiniDSP2x4HD,

        // -------------------------------------------------------------------------
        // AVRs and processors -----------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Processors supporting Dirac Live.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLive,
        /// <summary>
        /// Processors supporting Audyssey MultEQ-X.
        /// </summary>
        MultEQX,
        /// <summary>
        /// Emotiva XMC processors.
        /// </summary>
        Emotiva,
        /// <summary>
        /// StormAudio ISP processors.
        /// </summary>
        StormAudio,

        // -------------------------------------------------------------------------
        // Amplifiers --------------------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Behringer NX-series stereo amplifiers.
        /// </summary>
        BehringerNX,
    }
}