using System;
using System.Xml.Linq;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// An ADM element with an ID and a name.
    /// </summary>
    public abstract class TaggedADMElement {
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
        public TaggedADMElement() { }

        /// <summary>
        /// An ADM element with a set ID and name.
        /// </summary>
        public TaggedADMElement(string id, string name) {
            ID = id;
            Name = name;
        }

        /// <summary>
        /// Displays the ID and the name of the element.
        /// </summary>
        public override string ToString() => $"[{ID}] {Name}";

        /// <summary>
        /// Convert a timestamp to samples if its attribute is present.
        /// </summary>
        protected static TimeSpan ParseTimestamp(XAttribute attribute) =>
            attribute != null ? TimeSpan.Parse(attribute.Value) : default;
    }
}