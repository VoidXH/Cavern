using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Cavern.Filters {
    /// <summary>
    /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
    /// </summary>
    public class BypassFilter : Filter, IXmlSerializable {
        /// <summary>
        /// Name of this filter node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
        /// </summary>
        /// <param name="name">Name of this filter node</param>
        public BypassFilter(string name) => Name = name;

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            // Bypass
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            // Bypass
        }

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public virtual void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                if (reader.Name == nameof(Name)) {
                    Name = reader.Value;
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(BypassFilter));
            writer.WriteAttributeString(nameof(Name), Name);
            writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public override object Clone() => new BypassFilter(Name);

        /// <inheritdoc/>
        public override string ToString() => Name;
    }
}
