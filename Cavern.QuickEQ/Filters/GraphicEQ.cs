using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

using Cavern.Filters.Interfaces;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Converts an <see cref="Equalizer"/> to a convolution filter.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ library and is not available in the Cavern library's Filters namespace,
    /// because it requires QuickEQ library functions.</remarks>
    public class GraphicEQ : FastConvolver, IEqualizerAPOFilter {
        /// <summary>
        /// Copy of the equalizer curve for further alignment.
        /// </summary>
        /// <remarks>Changing the bands on this <see cref="Equalizer"/> does not result in the recomputation of the convolution filter,
        /// please use the setter instead.</remarks>
        public Equalizer Equalizer {
            get => equalizer;
            set {
                Impulse = value.GetConvolution(SampleRate, Length);
                equalizer = value;
            }
        }
        Equalizer equalizer;

        /// <summary>
        /// Get a clone of the <see cref="filter"/>'s impulse response.
        /// </summary>
        [IgnoreDataMember]
        public new float[] Impulse {
            get => base.Impulse;
            set => base.Impulse = value;
        }

        /// <summary>
        /// Added filter delay to the impulse, in samples.
        /// </summary>
        [IgnoreDataMember]
        public new int Delay {
            get => 0;
            set => throw new InvalidOperationException();
        }

        /// <summary>
        /// Convert an <paramref name="equalizer"/> to a 65536-sample convolution filter.
        /// </summary>
        /// <param name="equalizer">Desired frequency response change</param>
        /// <param name="sampleRate">Sample rate at which this EQ is converted to a minimum-phase FIR filter</param>
        public GraphicEQ(Equalizer equalizer, int sampleRate) : this(equalizer, sampleRate, 65536) { }

        /// <summary>
        /// Convert an <paramref name="equalizer"/> to a convolution filter.
        /// </summary>
        /// <param name="equalizer">Desired frequency response change</param>
        /// <param name="sampleRate">Sample rate at which this EQ is converted to a minimum-phase FIR filter</param>
        /// <param name="filterLength">Number of samples in the generated convolution filter, must be a power of 2</param>
        public GraphicEQ(Equalizer equalizer, int sampleRate, int filterLength) :
            base(equalizer.GetConvolution(sampleRate, filterLength)) {
            this.equalizer = equalizer;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Convert an <paramref name="equalizer"/> to a convolution filter, with optimal performance.
        /// </summary>
        /// <param name="equalizer">Desired frequency response change</param>
        /// <param name="sampleRate">Sample rate at which this EQ is converted to a minimum-phase FIR filter</param>
        /// <param name="cache">Reused FFT helper values for better performance - use the non-cache version for auto-calculation</param>
        public GraphicEQ(Equalizer equalizer, int sampleRate, FFTCache cache) :
            base(equalizer.GetConvolution(sampleRate, cache)) {
            this.equalizer = equalizer;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO to a Cavern <see cref="GraphicEQ"/> filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphicEQ FromEqualizerAPO(string line, int sampleRate) =>
            FromEqualizerAPO(line.Split(' ', StringSplitOptions.RemoveEmptyEntries), sampleRate);

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO which was split at spaces to a Cavern <see cref="GraphicEQ"/> filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphicEQ FromEqualizerAPO(string[] splitLine, int sampleRate) =>
            new GraphicEQ(EQGenerator.FromEqualizerAPO(splitLine), sampleRate);

        /// <inheritdoc/>
        public override object Clone() => new GraphicEQ((Equalizer)equalizer.Clone(), SampleRate);

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(SampleRate):
                        SampleRate = int.Parse(reader.Value);
                        break;
                    case nameof(Equalizer):
                        Equalizer = EQGenerator.FromEqualizerAPO(reader.Value);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(GraphicEQ));
            writer.WriteAttributeString(nameof(SampleRate), SampleRate.ToString());
            writer.WriteAttributeString(nameof(Equalizer), equalizer.ExportToEqualizerAPO());
            writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void ExportToEqualizerAPO(List<string> wipConfig) => wipConfig.Add(equalizer.ExportToEqualizerAPO());

        /// <inheritdoc/>
        public override string ToString() =>
            $"Graphic EQ: {Equalizer.Bands.Count} bands, {QMath.ToStringLimitDecimals(Equalizer.PeakGain, 3)} dB peak";

        /// <inheritdoc/>
        public override string ToString(CultureInfo culture) {
            string roundedPeak = QMath.ToStringLimitDecimals(Equalizer.PeakGain, 3);
            return culture.Name switch {
                "hu-HU" => $"Grafikus EQ: {Equalizer.Bands.Count} sáv, {roundedPeak} dB csúcs",
                _ => ToString()
            };
        }
    }
}