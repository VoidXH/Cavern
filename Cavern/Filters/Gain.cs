using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Signal level multiplier filter.
    /// </summary>
    public class Gain : Filter, IEqualizerAPOFilter, ILocalizableToString, IXmlSerializable {
        /// <summary>
        /// Filter gain in decibels.
        /// </summary>
        [DisplayName("Gain (dB)")]
        public double GainValue {
            get => 20 * Math.Log10(Math.Abs(gainValue));
            set => gainValue = (float)Math.Pow(10, value * .05);
        }

        /// <summary>
        /// Invert the phase in addition to changing gain.
        /// </summary>
        public bool Invert {
            get => gainValue < 0;
            set => gainValue = value ? Math.Abs(gainValue) : -Math.Abs(gainValue);
        }

        /// <summary>
        /// Filter gain as a multiplier.
        /// </summary>
        float gainValue;

        /// <summary>
        /// Signal level multiplier filter.
        /// </summary>
        /// <param name="gain">Filter gain in decibels</param>
        public Gain(double gain) => GainValue = gain;

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            for (int sample = 0; sample < samples.Length; sample++) {
                samples[sample] *= gainValue;
            }
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            for (int sample = channel; sample < samples.Length; sample += channels) {
                samples[sample] *= gainValue;
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new Gain(GainValue) {
            Invert = Invert
        };

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(GainValue):
                        GainValue = QMath.ParseDouble(reader.Value);
                        break;
                    case nameof(Invert):
                        Invert = bool.Parse(reader.Value);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(Gain));
            writer.WriteAttributeString(nameof(GainValue), GainValue.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(nameof(Invert), Invert.ToString());
            writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public override string ToString() => $"Gain: {QMath.ToStringLimitDecimals(GainValue, 2)} dB";

        /// <inheritdoc/>
        public void ExportToEqualizerAPO(List<string> wipConfig) =>
            wipConfig.Add($"Preamp: {GainValue.ToString(CultureInfo.InvariantCulture)} dB");

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" => $"Erősítés: {QMath.ToStringLimitDecimals(GainValue, 2)} dB",
            _ => ToString()
        };
    }
}