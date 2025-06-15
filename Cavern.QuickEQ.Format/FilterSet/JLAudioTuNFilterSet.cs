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
        public override void Export(string path) => File.WriteAllText(path, Export(false));

        /// <inheritdoc/>
        protected override string Export(bool gainOnly) {
            JsonFile traceSettings = new JsonFile();
            for (int i = 0; i < Channels.Length; i++) {
                JsonFile trace = new JsonFile("Color", "#" + defaultColors[i % Channels.Length].ToString("x6"));
                traceSettings.Add("t_" + (i + 1), trace);
            }
            JsonFile file = new JsonFile() {
                { "Selected Target", "t_1" },
                { "Trace Settings", traceSettings }
            };

            for (int i = 0; i < Channels.Length; i++) {
                JsonFile[] eqBands = new JsonFile[Bands];
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int band = 0; band < filters.Length; band++) {
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
                    { "Name", Channels[i].name },
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
        static readonly KeyValuePair<string, object> crossover = "Crossovers".Stores(new JsonFile() {
            { "Hpf Freq", 20 },
            { "Hpf Type", "off" },
            { "Lpf Freq", 20000 },
            { "Lpf Type", "off" }
        });

        /// <summary>
        /// Default delay settings for each channel.
        /// </summary>
        static readonly KeyValuePair<string, object> delay = "Delay/Polarity".Stores(new JsonFile() {
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
        static readonly KeyValuePair<string, object> eqSettings = "EQ Settings".Stores(new JsonFile() {
            { "High Shelf", false },
            { "Low Shelf", false }
        });
    }
}
