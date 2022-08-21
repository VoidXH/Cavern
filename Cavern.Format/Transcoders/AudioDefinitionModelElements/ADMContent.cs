using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a group of objects of an ADM program.
    /// </summary>
    public sealed class ADMContent : TaggedADMElement {
        /// <summary>
        /// References to audio objects that are part of this content by ID.
        /// </summary>
        public List<string> Objects { get; set; }

        /// <summary>
        /// Contains a group of objects of an ADM program.
        /// </summary>
        public ADMContent(string id, string name) : base(id, name) { }

        /// <summary>
        /// Constructs a content from an XML element.
        /// </summary>
        public ADMContent(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override XElement Serialize(XNamespace ns) {
            XElement root = new XElement(ns + ADMTags.contentTag,
                new XAttribute(ADMTags.contentIDAttribute, ID),
                new XAttribute(ADMTags.contentNameAttribute, Name));
            SerializeStrings(Objects, root, ADMTags.objectRefTag);
            root.Add(new XElement(ns + ADMTags.contentDialogueTag,
                (int)ADMDialogueType.mixedContentKind,
                new XAttribute(ADMDialogueType.mixedContentKind.ToString(), (int)MixedContentKind.Undefined)));
            return root;
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.contentIDAttribute);
            Name = source.GetAttribute(ADMTags.contentNameAttribute);
            Objects = ParseStrings(source, ADMTags.objectRefTag);
        }
    }
}