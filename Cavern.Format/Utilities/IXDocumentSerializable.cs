using System.Xml.Linq;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// An object that can be serialized into an XDocument.
    /// </summary>
    public interface IXDocumentSerializable {
        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        void Serialize(XElement parent);
    }
}