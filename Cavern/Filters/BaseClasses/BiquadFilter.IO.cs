using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using Cavern.Utilities;

namespace Cavern.Filters {
    partial class BiquadFilter : IBase64Serializable, IXmlSerializable {
        /// <summary>
        /// Create a biquad filter from a base64 string.
        /// </summary>
        /// <param name="source">Filter data encoded with <see cref="ToBase64"/>.</param>
        /// <returns>The appropriate biquad filter instance.</returns>
        public static BiquadFilter FromBase64String(string source) {
            byte[] data = Convert.FromBase64String(source);
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            BiquadFilterType filterType = (BiquadFilterType)reader.ReadByte();
            int sampleRate = reader.ReadInt32();
            double centerFreq = reader.ReadDouble();
            double q = reader.ReadDouble();
            double gain = reader.ReadDouble();
            bool phaseSwapped = reader.ReadBoolean();

            BiquadFilter filter = filterType switch {
                BiquadFilterType.PeakingEQ => new PeakingEQ(sampleRate, centerFreq, q, gain),
                BiquadFilterType.Lowpass => new Lowpass(sampleRate, centerFreq, q, gain),
                BiquadFilterType.Highpass => new Highpass(sampleRate, centerFreq, q, gain),
                BiquadFilterType.Bandpass => new Bandpass(sampleRate, centerFreq, q, gain),
                BiquadFilterType.Notch => new Notch(sampleRate, centerFreq, q, gain),
                BiquadFilterType.LowShelf => new LowShelf(sampleRate, centerFreq, q, gain),
                BiquadFilterType.HighShelf => new HighShelf(sampleRate, centerFreq, q, gain),
                BiquadFilterType.Allpass => new Allpass(sampleRate, centerFreq, q, gain),
                _ => throw new NotImplementedException()
            };

            if (filter is PhaseSwappableBiquadFilter phaseSwappable) {
                phaseSwappable.PhaseSwapped = phaseSwapped;
            }

            return filter;
        }

        /// <inheritdoc/>
        public void FromBase64(string source) {
            byte[] data = Convert.FromBase64String(source);
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.ReadByte(); // filterType
            sampleRate = reader.ReadInt32();
            centerFreq = reader.ReadDouble();
            q = reader.ReadDouble();
            gain = reader.ReadDouble();
            Reset(centerFreq, q, gain);
            bool phaseSwapped = reader.ReadBoolean();
            if (phaseSwapped) {
                if (this is PhaseSwappableBiquadFilter phaseSwappable) {
                    phaseSwappable.PhaseSwapped = phaseSwapped;
                } else {
                    throw new InvalidDataException("Phase is swapped on a filter that doesn't support it.");
                }
            }
        }

        /// <inheritdoc/>
        public string ToBase64() {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write((byte)FilterType);
            writer.Write(SampleRate);
            writer.Write(CenterFreq);
            writer.Write(Q);
            writer.Write(Gain);
            writer.Write(this is PhaseSwappableBiquadFilter phaseSwappable && phaseSwappable.PhaseSwapped);
            return Convert.ToBase64String(ms.ToArray());
        }

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
