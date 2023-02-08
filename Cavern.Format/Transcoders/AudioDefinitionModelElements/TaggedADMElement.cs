using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// An ADM element with an ID and a name.
    /// </summary>
    public abstract class TaggedADMElement: IXDocumentSerializable {
        /// <summary>
        /// Identifier of the element.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Name of the element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// An ADM element with an empty ID and name.
        /// </summary>
        protected TaggedADMElement() { }

        /// <summary>
        /// An ADM element with a set ID and name.
        /// </summary>
        protected TaggedADMElement(string id, string name) {
            ID = id;
            Name = name;
        }

        /// <summary>
        /// An ADM element parsed from the corresponding XML element.
        /// </summary>
        protected TaggedADMElement(XElement source) => Deserialize(source);

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public abstract void Serialize(XmlWriter writer);

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public abstract void Deserialize(XElement source);

        /// <summary>
        /// Displays the ID and the name of the element.
        /// </summary>
        public override string ToString() => $"[{ID}] {Name}";

        /// <summary>
        /// Export all elements from a list of strings to child elements with a given name.
        /// This is used for exporting multiple references by ID.
        /// </summary>
        protected static void SerializeStrings(List<string> from, XmlWriter to, string elementName) {
            for (int i = 0, c = from.Count; i < c; i++) {
                to.WriteElementString(elementName, from[i]);
            }
        }

        /// <summary>
        /// Import all of the given element's instances' values to a list.
        /// </summary>
        protected static List<string> ParseStrings(XElement from, string what) {
            IEnumerable<XElement> children = from.AllDescendants(what);
            using IEnumerator<XElement> enumerator = children.GetEnumerator();
            List<string> result = new List<string>();
            while (enumerator.MoveNext()) {
                result.Add(enumerator.Current.Value);
            }
            return result;
        }

        /// <summary>
        /// Convert a timestamp to samples if its attribute is present.
        /// </summary>
        protected static ADMTimeSpan ParseTimestamp(XAttribute attribute) =>
            attribute != null ? new ADMTimeSpan(attribute.Value) : default;
    }
}