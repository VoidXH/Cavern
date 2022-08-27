﻿using System;
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
        public static IEnumerable<XElement> AllDescendants(this XElement element, string name) =>
            element.Descendants(XName.Get(name, element.Name.NamespaceName));

        /// <summary>
        /// Gets the value of an element's <paramref name="attribute"/> by name.
        /// </summary>
        public static string GetAttribute(this XElement element, string attribute) => element.Attribute(attribute).Value;

        /// <summary>
        /// Gets the value of an element's <paramref name="child"/> by name.
        /// </summary>
        public static string GetElement(this XElement element, string child) =>
            element.Element(XName.Get(child, element.Name.NamespaceName)).Value;

        /// <summary>
        /// Get a time span in a standard timestamp format.
        /// </summary>
        public static string GetTimestamp(this TimeSpan stamp) =>
            $"{stamp:hh\\:mm\\:ss}.{stamp.Ticks % TimeSpan.TicksPerSecond * 100000 / TimeSpan.TicksPerSecond:00000}";
    }
}