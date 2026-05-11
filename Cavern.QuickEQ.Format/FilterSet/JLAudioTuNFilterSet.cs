using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.JSON;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for JL Audio's TüN software.
    /// </summary>
    public class JLAudioTuNFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override string FileExtension => "jltarg";

        /// <inheritdoc/>
        public override int Bands => 10;

        /// <inheritdoc/>
        public override double MinGain => -30;

        /// <inheritdoc/>
        public override double MaxGain => 12;

        /// <summary>
        /// IIR filter set for JL Audio's TüN software.
        /// </summary>
        public JLAudioTuNFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for JL Audio's TüN software.
        /// </summary>
        public JLAudioTuNFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Convert a <see cref="BiquadFilter"/> or an empty filter to a JSON file element.
        /// </summary>
        static JsonFile ParseFilter(bool enabled, double freq, double gain, double q) => new JsonFile {
            { "Enabled", enabled },
            { "Freq", freq },
            { "Gain", gain },
            { "Q", q }
        };

        /// <inheritdoc/>
        public override void Export(string path) {
            if (Channels.Length <= 8) {
                File.WriteAllText(path, Export(0, Channels.Length));
            } else {
                string folder = Path.GetDirectoryName(path);
                string rootFileName = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                for (int offset = 0; offset < Channels.Length; offset += 8) {
                    string fileName = Path.Combine(folder, $"{rootFileName} {offset / 8 + 1}{extension}");
                    string segmentFile = Export(offset, Math.Min(8, Channels.Length - offset));
                    File.WriteAllText(fileName, segmentFile);
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>Returns a single configuration file without regarding the 8 channel upper limit per such file.</remarks>
        protected override string Export(bool gainOnly) => Export(0, Channels.Length);

        /// <summary>
        /// Export a <paramref name="count"/> of channels from the given starting <paramref name="offset"/>.
        /// This matters because TüN supports 8 channels per configuration file.
        /// </summary>
        string Export(int offset, int count) {
            JsonFile traceSettings = new JsonFile();
            for (int i = 0; i < count; i++) {
                JsonFile trace = new JsonFile("Color", "#" + defaultColors[i % defaultColors.Length].ToString("x6"));
                traceSettings.Add("t_" + (i + 1), trace);
            }
            JsonFile file = new JsonFile {
                { "Selected Target", "t_1" },
                { "Trace Settings", traceSettings }
            };

            for (int i = 0; i < count; i++) {
                JsonFile[] eqBands = new JsonFile[Bands];
                BiquadFilter[] filters = ((IIRChannelData)Channels[i + offset]).filters;
                for (int band = 0, bands = Math.Min(filters.Length, Bands); band < bands; band++) {
                    eqBands[band] = ParseFilter(true, filters[band].CenterFreq, filters[band].Gain, filters[band].Q);
                }
                for (int band = filters.Length; band < Bands; band++) {
                    eqBands[band] = ParseFilter(false, 20, 0, QFactor.reference);
                }

                file["t_" + (i + 1)] = new JsonFile {
                    crossover,
                    delay,
                    { "EQ Bands", eqBands },
                    eqSettings,
                    { "In sum", false },
                    { "Level Trim", 0 },
                    { "Mag Offset", 0 },
                    { "Name", Channels[i + offset].name },
                    { "Spectrum Offset", 0 },
                    { "Visible", true }
                };
            }

            return file.ToString();
        }

        /// <summary>
        /// Default colors for the channels at each index.
        /// </summary>
        static readonly int[] defaultColors = {
            0xcc0000, 0x949599, 0x36b449, 0x973996, 0x00adee, 0xec008d, 0x14a79d, 0x603813, 0xcccc00
        };

        /// <summary>
        /// Default disabled crossover settings for each channel.
        /// </summary>
        static readonly KeyValuePair<string, object> crossover = "Crossovers".Stores(new JsonFile {
            { "Hpf Freq", 20 },
            { "Hpf Type", "off" },
            { "Lpf Freq", 20000 },
            { "Lpf Type", "off" }
        });

        /// <summary>
        /// Default delay settings for each channel.
        /// </summary>
        static readonly KeyValuePair<string, object> delay = "Delay/Polarity".Stores(new JsonFile {
            { "Additional Delay", 0 },
            { "Apf Freq", 1000 },
            { "Apf Mode", "off" },
            { "Apf Q", QFactor.reference },
            { "Polarity", true },
            { "Speaker Distance", 0 }
        });

        /// <summary>
        /// Default EQ settings for each channel.
        /// </summary>
        static readonly KeyValuePair<string, object> eqSettings = "EQ Settings".Stores(new JsonFile {
            { "High Shelf", false },
            { "Low Shelf", false }
        });
    }
}
