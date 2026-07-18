using System.Globalization;
using System.Xml;
using System.Xml.Schema;

using Cavern.Utilities;

namespace Cavern.Filters {
    partial class BiquadFilter {

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(SampleRate):
                        sampleRate = int.Parse(reader.Value);
                        break;
                    case nameof(CenterFreq):
                        centerFreq = QMath.ParseDouble(reader.Value);
                        break;
                    case nameof(Q):
                        q = QMath.ParseDouble(reader.Value);
                        break;
                    case nameof(Gain):
                        gain = QMath.ParseDouble(reader.Value);
                        break;
                }
            }
            Reset(centerFreq, q, gain);
        }

        /// <inheritdoc/>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(FilterType.ToString());
            writer.WriteAttributeString(nameof(SampleRate), sampleRate.ToString());
            writer.WriteAttributeString(nameof(CenterFreq), centerFreq.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(nameof(Q), q.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(nameof(Gain), gain.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        /// <summary>
        /// Display the filter's parameters when converting to string.
        /// </summary>
        public override string ToString() => $"{FilterType} at {centerFreq} Hz, Q: {QMath.ToStringLimitDecimals(q, 3)}, gain: {QMath.ToStringLimitDecimals(gain, 2)} dB";

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" =>
            $"{FilterType} {centerFreq} Hz-en, Q: {QMath.ToStringLimitDecimals(q, 3)}, erősítés: {QMath.ToStringLimitDecimals(gain, 2)} dB",
            _ => ToString()
        };
    }
}
