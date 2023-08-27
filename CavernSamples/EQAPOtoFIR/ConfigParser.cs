using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format;
using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Equalization;

namespace EQAPOtoFIR {
    /// <summary>
    /// Parser for a complete Equalizer APO configuration file.
    /// </summary>
    public class ConfigParser {
        /// <summary>
        /// Each channel's parsed data.
        /// </summary>
        readonly EqualizedChannel[] result;

        /// <summary>
        /// Load an Equalizer APO configuration file and parse all channel's filters.
        /// </summary>
        public ConfigParser(string path) {
            string[] contents = File.ReadAllLines(path);
            string[] active = { "ALL" };
            Dictionary<string, EqualizedChannel> channels = new Dictionary<string, EqualizedChannel> {
                ["ALL"] = new EqualizedChannel("ALL")
            };
            for (int line = 0; line < contents.Length; line++) {
                if (string.IsNullOrWhiteSpace(contents[line])) {
                    continue;
                }
                if (contents[line].StartsWith("Channel:")) {
                    active = contents[line][(contents[line].IndexOf(' ') + 1)..].Trim().ToUpperInvariant().Split(' ');
                } else {
                    for (int ch = 0; ch < active.Length; ch++) {
                        if (!channels.ContainsKey(active[ch])) {
                            channels[active[ch]] = new EqualizedChannel(active[ch]);
                            channels[active[ch]].Modify(channels["ALL"].Result);
                            channels[active[ch]].Modify(channels["ALL"].Filters);
                        }
                        LineParser.Parse(contents[line], channels[active[ch]]);
                    }
                }
            }

            result = new EqualizedChannel[channels.Count];
            int i = 0;
            foreach (KeyValuePair<string, EqualizedChannel> channel in channels) {
                result[i++] = channel.Value;
            }
        }

        /// <summary>
        /// Export the filters as RIFF WAVE files.
        /// </summary>
        public void ExportWAV(string path, ExportFormat format, BitDepth bits, int sampleRate, int samples, bool minimumPhase) {
            for (int channel = 0; channel < result.Length; channel++) {
                using FileStream file =
                    File.Open(Path.Combine(path, string.Format("Channel_{0}.wav", result[channel].Name)), FileMode.Create);
                using RIFFWaveWriter writer = new RIFFWaveWriter(file, 1, samples, sampleRate, bits);
                result[channel].Export(writer, format, minimumPhase);
            }
        }

        /// <summary>
        /// Export the filters as C arrays.
        /// </summary>
        public void ExportC(string path, ExportFormat format, BitDepth bits, int sampleRate, int samples, bool minimumPhase, int blocks) {
            for (int channel = 0; channel < result.Length; channel++) {
                using FileStream file =
                    File.Open(Path.Combine(path, string.Format("Channel_{0}.c", result[channel].Name)), FileMode.Create);
                using CWriter writer = new CWriter(file, 1, samples, sampleRate, bits);
                result[channel].ExportInBlocks(writer, format, minimumPhase, samples / blocks);
            }
        }

        /// <summary>
        /// Export the filters as HLS additions.
        /// </summary>
        public void ExportHLS(string path, ExportFormat format, BitDepth bits, int sampleRate, int samples, bool minimumPhase) {
            for (int channel = 0; channel < result.Length; channel++) {
                using FileStream file =
                    File.Open(Path.Combine(path, string.Format("Channel_{0}.txt", result[channel].Name)), FileMode.Create);
                using HLSWriter writer = new HLSWriter(file, 1, samples, sampleRate, bits);
                result[channel].ExportInBlocks(writer, format, minimumPhase, 64);
            }
        }

        /// <summary>
        /// Handle MQX export.
        /// </summary>
        void ExportMQX(SaveFileDialog saver, Func<EqualizedChannel, BiquadFilter[]> getter, int sampleRate) {
            MultEQXFilterSet set = new MultEQXFilterSet(result.Length, sampleRate);
            for (int channel = 0; channel < result.Length; channel++) {
                set.SetupChannel(EqualizerAPOUtils.GetReferenceChannel(result[channel].Name), getter(result[channel]));
            }
            if (saver.ShowDialog() == true) {
                try {
                    set.Export(saver.FileName);
                } catch {
                    MessageBox.Show("Invalid filters were used in Equalizer APO. Only use lowpass, low shelf, highpass, high shelf, " +
                        "or peaking EQ.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Export the filters for MultEQ-X.
        /// </summary>
        public void ExportMQX(SaveFileDialog saver, int sampleRate) => ExportMQX(saver, channel => channel.Filters.ToArray(), sampleRate);

        /// <summary>
        /// Export the filters and gains for MultEQ-X.
        /// </summary>
        public void ExportMQXSim(SaveFileDialog saver, int sampleRate) =>
            ExportMQX(saver, channel => new PeakingEqualizer(channel.Result).GetPeakingEQ(48000, 10), sampleRate);
    }
}