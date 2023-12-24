namespace Cavern.Format.FilterSet {
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
        /// Equalizer APO for Windows using EQ curves.
        /// </summary>
        EqualizerAPO_EQ,
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
        /// <summary>
        /// AU N-Band EQ for Mac.
        /// </summary>
        AUNBandEQ,

        // -------------------------------------------------------------------------
        // External DSP hardware ---------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// MiniDSP 2x4 Advanced plugin for the standard MiniDSP 2x4.
        /// </summary>
        MiniDSP2x4Advanced,
        /// <summary>
        /// MiniDSP 2x4 Advanced plugin for the standard MiniDSP 2x4, only using half the bands.
        /// </summary>
        MiniDSP2x4AdvancedLite,
        /// <summary>
        /// MiniDSP 2x4 HD hardware DSP.
        /// </summary>
        MiniDSP2x4HD,
        /// <summary>
        /// MiniDSP 2x4 HD hardware DSP, only using half the bands.
        /// </summary>
        MiniDSP2x4HDLite,
        /// <summary>
        /// MiniDSP DDRC-88A hardware DSP.
        /// </summary>
        MiniDSPDDRC88A,

        // -------------------------------------------------------------------------
        // AVRs and processors -----------------------------------------------------
        // -------------------------------------------------------------------------
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

        // -------------------------------------------------------------------------
        // Others ------------------------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Processors supporting Dirac Live.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLive,
        /// <summary>
        /// Processors supporting Dirac Live Bass Control. DLBC requires some channels to be merged into groups.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLiveBassControl,
        /// <summary>
        /// Processors supporting Audyssey MultEQ-X, MultEQ-X config file.
        /// </summary>
        MultEQX,
        /// <summary>
        /// Processors supporting Audyssey MultEQ-X, PEQ files.
        /// </summary>
        MultEQXRaw,
        /// <summary>
        /// Processors supporting Audyssey MultEQ-X, target curve files.
        /// </summary>
        MultEQXTarget,
        /// <summary>
        /// Processors supporting the latest YPAO with additional fine tuning PEQs.
        /// </summary>
        YPAO,
    }
}