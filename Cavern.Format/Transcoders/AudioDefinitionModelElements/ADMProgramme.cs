using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Root element of the model's contained program.
    /// </summary>
    public sealed class ADMProgramme : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Length of the program in seconds.
        /// </summary>
        readonly double length;

        /// <summary>
        /// Groups of contained objects.
        /// </summary>
        public List<ADMContent> Contents { get; set; }

        /// <summary>
        /// Constructs a program of <paramref name="length"/> in seconds.
        /// </summary>
        public ADMProgramme(string id, string name, double length) : base(id, name) => this.length = length;

        /// <summary>
        /// Constructs a program from an XML element.
        /// </summary>
        public ADMProgramme(XElement source) => Deserialize(source);

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            XElement root = new XElement(parent.Name.Namespace + ADMTags.programTag,
                new XAttribute(ADMTags.programIDAttribute, ID),
                new XAttribute(ADMTags.programNameAttribute, Name),
                new XAttribute(ADMTags.startAttribute, new TimeSpan().GetTimestamp()),
                new XAttribute(ADMTags.programEndAttribute, TimeSpan.FromSeconds(length).GetTimestamp()));
            parent.Add(root);
            foreach (ADMContent content in Contents) {
                root.Add(new XElement(parent.Name.Namespace + ADMTags.contentRefTag, content.ID));
                content.Serialize(parent);
            }
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.programIDAttribute);
            Name = source.GetAttribute(ADMTags.programNameAttribute);
            // TODO: content references
        }
    }
}