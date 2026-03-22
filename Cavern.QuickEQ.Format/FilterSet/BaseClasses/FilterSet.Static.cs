using System;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    partial class FilterSet {
        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, int channels, int sampleRate) {
            return device switch {
                FilterSetTarget.Generic => new IIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericConvolution => new FIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericEqualizer => new EqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_EQ => new EqualizerAPOEqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR_Stereo => new EqualizerAPOFIRFilterSet(channels, sampleRate) {
                    ChannelsPerConvolution = 2
                },
                FilterSetTarget.EqualizerAPO_IIR => new EqualizerAPOIIRFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                FilterSetTarget.AUNBandEQ => new AUNBandEQ(channels, sampleRate),
                FilterSetTarget.MiniDSP2x4Advanced => new MiniDSP2x4FilterSet(channels),
                FilterSetTarget.MiniDSP2x4AdvancedLite => new MiniDSP2x4FilterSetLite(channels),
                FilterSetTarget.MiniDSP2x4HD => new MiniDSP2x4HDFilterSet(channels),
                FilterSetTarget.MiniDSP2x4HDLite => new MiniDSP2x4HDFilterSetLite(channels),
                FilterSetTarget.MiniDSPDDRC88A => new MiniDSPDDRC88AFilterSet(channels),
                FilterSetTarget.MiniDSPFlexHTx => new MiniDSPFlexHTxFilterSet(channels),
                FilterSetTarget.AcurusMuse => new AcurusMuseFilterSet(channels, sampleRate),
                FilterSetTarget.Emotiva => new EmotivaFilterSet(channels, sampleRate),
                FilterSetTarget.MonolithHTP1 => new MonolithHTP1FilterSet(channels, sampleRate),
                FilterSetTarget.Rotel => new RotelFilterSet(channels, sampleRate),
                FilterSetTarget.SonyES => new SonyESSeriesFilterSet(channels, sampleRate),
                FilterSetTarget.StormAudio => new StormAudioFilterSet(channels, sampleRate),
                FilterSetTarget.TonewinnerAT => new TonewinnerATFilterSet(channels, sampleRate),
                FilterSetTarget.WiiM => new WiiMFilterSet(channels, sampleRate),
                FilterSetTarget.BehringerNX => new BehringerNXFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLive => new DiracLiveFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveLimitedRange => new DiracLiveLimitedRangeFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControl => new DiracLiveBassControlFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControlCombined => new DiracLiveBassControlFilterSet(channels, sampleRate) {
                    CombineHeights = true
                },
                FilterSetTarget.JLAudioTun => new JLAudioTuNFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQX => new MultEQXFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXRaw => new MultEQXRawFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXTarget => new MultEQXTargetFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXT32 => new MultEQXT32FilterSet(channels, sampleRate),
                FilterSetTarget.YamahaRXA => new YamahaRXAFilterSet(channels, sampleRate),
                FilterSetTarget.YPAO => new YPAOFilterSet(channels, sampleRate),
                FilterSetTarget.YPAOLite => new YPAOLiteFilterSet(channels, sampleRate),
                FilterSetTarget.Multiband10 => new Multiband10FilterSet(channels, sampleRate),
                FilterSetTarget.Multiband31 => new Multiband31FilterSet(channels, sampleRate),
                FilterSetTarget.Roon => new RoonFilterSet(channels),
                FilterSetTarget.Wavelet => new WaveletFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, ReferenceChannel[] channels, int sampleRate) {
            return device switch {
                FilterSetTarget.Generic => new IIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericConvolution => new FIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericEqualizer => new EqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_EQ => new EqualizerAPOEqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR_Stereo => new EqualizerAPOFIRFilterSet(channels, sampleRate) {
                    ChannelsPerConvolution = 2
                },
                FilterSetTarget.EqualizerAPO_IIR => new EqualizerAPOIIRFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                FilterSetTarget.AUNBandEQ => new AUNBandEQ(channels, sampleRate),
                FilterSetTarget.MiniDSP2x4Advanced => new MiniDSP2x4FilterSet(channels),
                FilterSetTarget.MiniDSP2x4AdvancedLite => new MiniDSP2x4FilterSetLite(channels),
                FilterSetTarget.MiniDSP2x4HD => new MiniDSP2x4HDFilterSet(channels),
                FilterSetTarget.MiniDSP2x4HDLite => new MiniDSP2x4HDFilterSetLite(channels),
                FilterSetTarget.MiniDSPDDRC88A => new MiniDSPDDRC88AFilterSet(channels),
                FilterSetTarget.MiniDSPFlexHTx => new MiniDSPFlexHTxFilterSet(channels),
                FilterSetTarget.AcurusMuse => new AcurusMuseFilterSet(channels, sampleRate),
                FilterSetTarget.Emotiva => new EmotivaFilterSet(channels, sampleRate),
                FilterSetTarget.MonolithHTP1 => new MonolithHTP1FilterSet(channels, sampleRate),
                FilterSetTarget.Rotel => new RotelFilterSet(channels, sampleRate),
                FilterSetTarget.SonyES => new SonyESSeriesFilterSet(channels, sampleRate),
                FilterSetTarget.StormAudio => new StormAudioFilterSet(channels, sampleRate),
                FilterSetTarget.TonewinnerAT => new TonewinnerATFilterSet(channels, sampleRate),
                FilterSetTarget.WiiM => new WiiMFilterSet(channels, sampleRate),
                FilterSetTarget.BehringerNX => new BehringerNXFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLive => new DiracLiveFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveLimitedRange => new DiracLiveLimitedRangeFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControl => new DiracLiveBassControlFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControlCombined => new DiracLiveBassControlFilterSet(channels, sampleRate) {
                    CombineHeights = true
                },
                FilterSetTarget.JLAudioTun => new JLAudioTuNFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQX => new MultEQXFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXRaw => new MultEQXRawFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXTarget => new MultEQXTargetFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXT32 => new MultEQXT32FilterSet(channels, sampleRate),
                FilterSetTarget.YamahaRXA => new YamahaRXAFilterSet(channels, sampleRate),
                FilterSetTarget.YPAO => new YPAOFilterSet(channels, sampleRate),
                FilterSetTarget.YPAOLite => new YPAOLiteFilterSet(channels, sampleRate),
                FilterSetTarget.Multiband10 => new Multiband10FilterSet(channels, sampleRate),
                FilterSetTarget.Multiband31 => new Multiband31FilterSet(channels, sampleRate),
                FilterSetTarget.Roon => new RoonFilterSet(channels),
                FilterSetTarget.Wavelet => new WaveletFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException()
            };
        }
    }
}