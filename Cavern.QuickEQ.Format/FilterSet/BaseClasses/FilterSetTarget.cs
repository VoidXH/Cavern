﻿using System;

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
            FilterSetTarget.GenericConvolution => "Generic Convolution",
            FilterSetTarget.GenericEqualizer => "Generic Equalizer",
            FilterSetTarget.EqualizerAPO_EQ => "Equalizer APO - graphic EQ",
            FilterSetTarget.EqualizerAPO_FIR => "Equalizer APO - convolution",
            FilterSetTarget.EqualizerAPO_IIR => "Equalizer APO - peaking EQ",
            FilterSetTarget.CamillaDSP => "CamillaDSP - convolution",
            FilterSetTarget.AUNBandEQ => "AU N-Band EQ",
            FilterSetTarget.MiniDSP2x4Advanced => "MiniDSP 2x4 Adv.",
            FilterSetTarget.MiniDSP2x4AdvancedLite => "MiniDSP 2x4 Adv. Lite",
            FilterSetTarget.MiniDSP2x4HD => "MiniDSP 2x4 HD",
            FilterSetTarget.MiniDSP2x4HDLite => "MiniDSP 2x4 HD Lite",
            FilterSetTarget.MiniDSPDDRC88A => "MiniDSP DDRC-88A",
            FilterSetTarget.MiniDSPFlexHTx => "MiniDSP Flex HTx",
            FilterSetTarget.AcurusMuse => "Acurus Muse",
            FilterSetTarget.Emotiva => "Emotiva",
            FilterSetTarget.MonolithHTP1 => "Monoprice Monolith HTP-1",
            FilterSetTarget.SonyES => "Sony ES series",
            FilterSetTarget.StormAudio => "StormAudio",
            FilterSetTarget.TonewinnerAT => "Tonewinner AT series",
            FilterSetTarget.WiiM => "WiiM",
            FilterSetTarget.BehringerNX => "Behringer NX series",
            FilterSetTarget.DiracLive => null,
            FilterSetTarget.DiracLiveBassControl => null,
            FilterSetTarget.DiracLiveBassControlCombined => null,
            FilterSetTarget.MultEQX => "MultEQ-X - MQX file",
            FilterSetTarget.MultEQXRaw => "MultEQ-X - peaking EQ",
            FilterSetTarget.MultEQXTarget => "MultEQ-X - filter curves",
            FilterSetTarget.YamahaRXA => "Yamaha RX-A series",
            FilterSetTarget.YPAO => "Yamaha RX series",
            FilterSetTarget.YPAOLite => "Yamaha CX-A series",
            FilterSetTarget.Multiband31 => "31-band graphic EQ",
            FilterSetTarget.Roon => "Roon",
            FilterSetTarget.Wavelet => "Wavelet",
            _ => throw new NotSupportedException()
        };
    }
}