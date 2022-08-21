using System.Xml.Linq;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// An object that can be serialized into an XDocument.
    /// </summary>
    public interface IXDocumentSerializable {
        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        XElement Serialize(XNamespace ns);

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        void Deserialize(XElement source);
    }
}