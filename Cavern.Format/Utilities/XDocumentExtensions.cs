using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Extension functions for <see cref="XDocument"/>.
    /// </summary>
    static class XDocumentExtensions {
        /// <summary>
        /// Gets the descendants that match a <paramref name="name"/>, disregarding the namespace.
        /// </summary>
        public static IEnumerable<XElement> AllDescendants(this XDocument document, string name) =>
            document.Descendants(XName.Get(name, document.Root.Name.NamespaceName));

        /// <summary>
        /// Gets the descendants that match a <paramref name="name"/>, disregarding the namespace.
        /// </summary>
        public static IEnumerable<XElement> AllDescendants(this XElement element, string name) =>
            element.Descendants(XName.Get(name, element.Name.NamespaceName));

        /// <summary>
        /// Gets the value of an element's <paramref name="attribute"/> by name.
        /// </summary>
        public static string GetAttribute(this XElement element, string attribute) => element.Attribute(attribute).Value;

        /// <summary>
        /// Get the <paramref name="tag"/> that has an <paramref name="attribute"/> with a given <paramref name="value"/>.
        /// </summary>
        public static XElement GetWithAttribute(this XDocument document, string tag, string attribute, string value) {
            IEnumerable<XElement> descendants = document.AllDescendants(tag);
            using IEnumerator<XElement> enumerator = descendants.GetEnumerator();
            while (enumerator.MoveNext()) {
                XAttribute attributeElement = enumerator.Current.Attribute(attribute);
                if (attributeElement.Value.Equals(value)) {
                    return enumerator.Current;
                }
            }
            return null;
        }

        /// <summary>
        /// Get a time span in a standard timestamp format.
        /// </summary>
        public static string GetTimestamp(this TimeSpan stamp) => stamp.ToString("c").Replace(',', '.');
    }
}