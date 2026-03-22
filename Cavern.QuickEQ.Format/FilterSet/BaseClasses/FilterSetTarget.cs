using System;

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
        /// IIR filter sets in the commonly accepted WAVE file format for maximum compatibility.
        /// </summary>
        GenericConvolution,
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
        /// Equalizer APO for Windows using convolution filters, channel pairs in filters instead of single channels.
        /// </summary>
        EqualizerAPO_FIR_Stereo,
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
        /// <summary>
        /// MiniDSP Flex HTx hardware DSP.
        /// </summary>
        MiniDSPFlexHTx,

        // -------------------------------------------------------------------------
        // AVRs and processors -----------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Acurus Muse processors.
        /// </summary>
        AcurusMuse,
        /// <summary>
        /// Emotiva XMC processors.
        /// </summary>
        Emotiva,
        /// <summary>
        /// Monoprice Monolith HTP-1 processors.
        /// </summary>
        MonolithHTP1,
        /// <summary>
        /// Rotel devices.
        /// </summary>
        Rotel,
        /// <summary>
        /// Sony ES-series AVRs.
        /// </summary>
        SonyES,
        /// <summary>
        /// StormAudio ISP processors.
        /// </summary>
        StormAudio,
        /// <summary>
        /// Tonewinner AT-series processors.
        /// </summary>
        TonewinnerAT,
        /// <summary>
        /// WiiM devices.
        /// </summary>
        WiiM,

        // -------------------------------------------------------------------------
        // Amplifiers --------------------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Behringer NX-series stereo amplifiers.
        /// </summary>
        BehringerNX,

        // -------------------------------------------------------------------------
        // Room correction software ------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Processors supporting Dirac Live.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLive,
        /// <summary>
        /// Processors supporting Dirac Live with only the Limited Range version available (500 Hz cutoff).
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLiveLimitedRange,
        /// <summary>
        /// Processors supporting Dirac Live Bass Control. DLBC requires some channels to be merged into groups.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLiveBassControl,
        /// <summary>
        /// Processors supporting Dirac Live Bass Control. DLBC requires some channels to be merged into groups.
        /// This version of DLBC merges even more than the regular, all heights are a single group.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLiveBassControlCombined,
        /// <summary>
        /// JL Audio's TüN software.
        /// </summary>
        JLAudioTun,
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
        /// Audyssey MultEQ XT32 configuration (.ady).
        /// </summary>
        MultEQXT32,
        /// <summary>
        /// Yamaha RX-A series AVRs.
        /// </summary>
        YamahaRXA,
        /// <summary>
        /// Yamaha RX series AVRs.
        /// </summary>
        YPAO,
        /// <summary>
        /// Yamaha CX-A series AVRs.
        /// </summary>
        YPAOLite,

        // -------------------------------------------------------------------------
        // Others ------------------------------------------------------------------
        // -------------------------------------------------------------------------
        /// <summary>
        /// Traditional 10-band graphic equalizer.
        /// </summary>
        Multiband10,
        /// <summary>
        /// Traditional 31-band graphic equalizer.
        /// </summary>
        Multiband31,
        /// <summary>
        /// Roon multi-sample rate convolution.
        /// </summary>
        Roon,
        /// <summary>
        /// Wavelet Android app.
        /// </summary>
        Wavelet,
    }
}
