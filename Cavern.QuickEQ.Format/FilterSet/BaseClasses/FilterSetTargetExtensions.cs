using System;

namespace Cavern.Format.FilterSet {
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
            FilterSetTarget.EqualizerAPO_FIR => "Equalizer APO - convolution (per channel)",
            FilterSetTarget.EqualizerAPO_FIR_Stereo => "Equalizer APO - convolution (per stereo)",
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
            FilterSetTarget.Rotel => "Rotel",
            FilterSetTarget.SonyES => "Sony ES series",
            FilterSetTarget.StormAudio => "StormAudio",
            FilterSetTarget.TonewinnerAT => "Tonewinner AT series",
            FilterSetTarget.WiiM => "WiiM",
            FilterSetTarget.BehringerNX => "Behringer NX series",
            FilterSetTarget.DiracLive => null,
            FilterSetTarget.DiracLiveLimitedRange => null,
            FilterSetTarget.DiracLiveBassControl => null,
            FilterSetTarget.DiracLiveBassControlCombined => null,
            FilterSetTarget.JLAudioTun => "JL Audio TüN",
            FilterSetTarget.MultEQX => "MultEQ-X - MQX file",
            FilterSetTarget.MultEQXRaw => "MultEQ-X - peaking EQ",
            FilterSetTarget.MultEQXTarget => "MultEQ-X - filter curves",
            FilterSetTarget.MultEQXT32 => "MultEQ XT32",
            FilterSetTarget.YamahaRXA => "Yamaha RX-A series",
            FilterSetTarget.YPAO => "Yamaha RX series",
            FilterSetTarget.YPAOLite => "Yamaha CX-A series",
            FilterSetTarget.Multiband10 => "10-band graphic EQ",
            FilterSetTarget.Multiband31 => "31-band graphic EQ",
            FilterSetTarget.Roon => "Roon",
            FilterSetTarget.Wavelet => "Wavelet",
            _ => throw new NotSupportedException()
        };
    }
}
