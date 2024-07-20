using System.Xml;

using Cavern.Utilities;

namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// Common functions of <see cref="IConvolution"/>s.
    /// </summary>
    public static class IConvolutionExtensions {
        /// <summary>
        /// Read an <see cref="IConvolution"/> from an XML, if only the common properties are required.
        /// </summary>
        public static void ReadCommonXml(this IConvolution conv, XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(conv.SampleRate):
                        conv.SampleRate = int.Parse(reader.Value);
                        break;
                    case nameof(conv.Delay):
                        conv.Delay = int.Parse(reader.Value);
                        break;
                    case nameof(conv.Impulse):
                        conv.Impulse = EncodingUtils.Base64ToFloatArray(reader.Value);
                        break;
                }
            }
        }

        /// <summary>
        /// Write an <see cref="IConvolution"/> to an XML, if only the common properties are required.
        /// </summary>
        public static void WriteCommonXml(this IConvolution conv, XmlWriter writer, string name) {
            writer.WriteStartElement(name);
            writer.WriteAttributeString(nameof(conv.SampleRate), conv.SampleRate.ToString());
            writer.WriteAttributeString(nameof(conv.Delay), conv.Delay.ToString());
            writer.WriteAttributeString(nameof(conv.Impulse), EncodingUtils.ToBase64(conv.Impulse));
            writer.WriteEndElement();
        }
    }
}