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
        /// Monoprice Monolith HTP-1 processors.
        /// </summary>
        MonolithHTP1,
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
        /// Processors supporting Dirac Live Bass Control. DLBC requires some channels to be merged into groups.
        /// This version of DLBC merges even more than the regular, all heights are a single group.
        /// </summary>
        /// <remarks>Dirac has no full override, only delta measurements are supported.</remarks>
        DiracLiveBassControlCombined,
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

    /// <summary>
    /// Extension functions for the <see cref="FilterSetTarget"/> enum.
    /// </summary>
    public static class FilterSetTargetExtensions {
        /// <summary>
        /// Convert the <paramref name="target"/> device to its name.
        /// </summary>
        public static string GetDeviceName(this FilterSetTarget target) => GetDeviceNameSafe(target) ?? throw new DeltaSetException();

        /// <summary>
        /// Convert the <paramref name="target"/> device to its name, and return null when the device is not available for single-measurement
        /// export, allowing for easier filtering of targets.
        /// </summary>
        public static string GetDeviceNameSafe(this FilterSetTarget target) => target switch {
            FilterSetTarget.Generic => "Generic Peaking EQ",
            FilterSetTarget.GenericEqualizer => "Generic Equalizer",
            FilterSetTarget.EqualizerAPO_EQ => "Equalizer APO - Graphic EQ",
            FilterSetTarget.EqualizerAPO_FIR => "Equalizer APO - Convolution",
            FilterSetTarget.EqualizerAPO_IIR => "Equalizer APO - Peaking EQ",
            FilterSetTarget.CamillaDSP => "CamillaDSP - Convolution",
            FilterSetTarget.AUNBandEQ => "AU N-Band EQ",
            FilterSetTarget.MiniDSP2x4Advanced => "MiniDSP 2x4 Adv.",
            FilterSetTarget.MiniDSP2x4AdvancedLite => "MiniDSP 2x4 Adv. Lite",
            FilterSetTarget.MiniDSP2x4HD => "MiniDSP 2x4 HD",
            FilterSetTarget.MiniDSP2x4HDLite => "MiniDSP 2x4 HD Lite",
            FilterSetTarget.MiniDSPDDRC88A => "MiniDSP DDRC-88A",
            FilterSetTarget.Emotiva => "Emotiva",
            FilterSetTarget.MonolithHTP1 => "Monoprice Monolith HTP-1",
            FilterSetTarget.StormAudio => "StormAudio",
            FilterSetTarget.BehringerNX => "Behringer NX series",
            FilterSetTarget.DiracLive => null,
            FilterSetTarget.DiracLiveBassControl => null,
            FilterSetTarget.DiracLiveBassControlCombined => null,
            FilterSetTarget.MultEQX => "MultEQ-X - MQX file",
            FilterSetTarget.MultEQXRaw => "MultEQ-X - Peaking EQ",
            FilterSetTarget.MultEQXTarget => "MultEQ-X - Filter curves",
            FilterSetTarget.YPAO => "YPAO",
            _ => throw new NotSupportedException()
        };
    }
}