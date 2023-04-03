using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set containing equalization info for each channel of a system.
    /// </summary>
    public abstract class FilterSet {
        /// <summary>
        /// Basic information needed for a channel.
        /// </summary>
        protected abstract class ChannelData {
            /// <summary>
            /// The reference channel describing this channel or <see cref="ReferenceChannel.Unknown"/> if not applicable.
            /// </summary>
            public ReferenceChannel reference;

            /// <summary>
            /// Custom label for this channel or null if not applicable.
            /// </summary>
            public string name;

            /// <summary>
            /// Delay of this channel in samples.
            /// </summary>
            public int delaySamples;
        }

        /// <summary>
        /// Applied filters for each channel in the configuration file.
        /// </summary>
        protected ChannelData[] Channels { get; set; }

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
        protected FilterSet(int sampleRate) => SampleRate = sampleRate;

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public abstract void Export(string path);

        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, int channels, int sampleRate) {
            return device switch {
                FilterSetTarget.Generic => new IIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericEqualizer => new EqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_EQ => new EqualizerAPOEqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_IIR => new EqualizerAPOIIRFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                FilterSetTarget.AUNBandEQ => new AUNBandEQ(channels, sampleRate),
                FilterSetTarget.MiniDSP2x4Advanced => new MiniDSP2x4FilterSet(channels),
                FilterSetTarget.MiniDSP2x4AdvancedLite => new MiniDSP2x4FilterSetLite(channels),
                FilterSetTarget.MiniDSP2x4HD => new MiniDSP2x4HDFilterSet(channels),
                FilterSetTarget.MiniDSP2x4HDLite => new MiniDSP2x4HDFilterSetLite(channels),
                FilterSetTarget.Emotiva => new EmotivaFilterSet(channels, sampleRate),
                FilterSetTarget.StormAudio => new StormAudioFilterSet(channels, sampleRate),
                FilterSetTarget.BehringerNX => new BehringerNXFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLive => new DiracLiveFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControl => new DiracLiveBassControlFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQX => new MultEQXFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXRaw => new MultEQXRawFilterSet(channels, sampleRate),
                FilterSetTarget.YPAO => new YPAOFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// Create a filter set for the target <paramref name="device"/>.
        /// </summary>
        public static FilterSet Create(FilterSetTarget device, ReferenceChannel[] channels, int sampleRate) {
            return device switch {
                FilterSetTarget.Generic => new IIRFilterSet(channels, sampleRate),
                FilterSetTarget.GenericEqualizer => new EqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_EQ => new EqualizerAPOEqualizerFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_FIR => new EqualizerAPOFIRFilterSet(channels, sampleRate),
                FilterSetTarget.EqualizerAPO_IIR => new EqualizerAPOIIRFilterSet(channels, sampleRate),
                FilterSetTarget.CamillaDSP => new CamillaDSPFilterSet(channels, sampleRate),
                FilterSetTarget.AUNBandEQ => new AUNBandEQ(channels, sampleRate),
                FilterSetTarget.MiniDSP2x4Advanced => new MiniDSP2x4FilterSet(channels),
                FilterSetTarget.MiniDSP2x4AdvancedLite => new MiniDSP2x4FilterSetLite(channels),
                FilterSetTarget.MiniDSP2x4HD => new MiniDSP2x4HDFilterSet(channels),
                FilterSetTarget.MiniDSP2x4HDLite => new MiniDSP2x4HDFilterSetLite(channels),
                FilterSetTarget.Emotiva => new EmotivaFilterSet(channels, sampleRate),
                FilterSetTarget.StormAudio => new StormAudioFilterSet(channels, sampleRate),
                FilterSetTarget.BehringerNX => new BehringerNXFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLive => new DiracLiveFilterSet(channels, sampleRate),
                FilterSetTarget.DiracLiveBassControl => new DiracLiveBassControlFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQX => new MultEQXFilterSet(channels, sampleRate),
                FilterSetTarget.MultEQXRaw => new MultEQXRawFilterSet(channels, sampleRate),
                FilterSetTarget.YPAO => new YPAOFilterSet(channels, sampleRate),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected virtual string GetLabel(int channel) => Channels[channel].name ?? "CH" + (channel + 1);

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        protected virtual void RootFileExtension(int channel, List<string> result) { }

        /// <summary>
        /// Get the delay for a given channel in milliseconds instead of samples.
        /// </summary>
        protected double GetDelay(int channel) => Channels[channel].delaySamples * 1000.0 / SampleRate;

        /// <summary>
        /// Create the file with gain/delay/polarity info as the root document that's saved in the save dialog.
        /// </summary>
        protected void CreateRootFile(string path, string filterFileExtension) {
            string fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            List<string> result = new List<string>();
            bool hasAnything = false,
                hasDelays = false;
            for (int i = 0, c = Channels.Length; i < c; i++) {
                result.Add(string.Empty);
                result.Add("Channel: " + GetLabel(i));
                if (Channels[i].delaySamples != 0) {
                    result.Add("Delay: " + GetDelay(i).ToString("0.0 ms"));
                    hasAnything = true;
                    hasDelays = true;
                }
                int before = result.Count;
                RootFileExtension(i, result);
                hasAnything |= result.Count != before;
            }
            if (hasAnything) {
                File.WriteAllLines(path, result.Prepend(hasDelays ?
                    $"Set up levels and delays by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ." :
                    $"Set up levels by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ."));
            }
        }
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
        /// Processors supporting the latest YPAO with additional fine tuning PEQs.
        /// </summary>
        YPAO,
    }
}