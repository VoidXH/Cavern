using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a group of objects of an ADM program.
    /// </summary>
    public sealed class ADMContent : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Audio objects that are part of this content.
        /// </summary>
        public List<ADMObject> Objects { get; set; }

        /// <summary>
        /// Contains a group of objects of an ADM program.
        /// </summary>
        public ADMContent(string id, string name) : base(id, name) { }

        /// <summary>
        /// Constructs a content from an XML element.
        /// </summary>
        public ADMContent(XElement source) => Deserialize(source);

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            XElement root = new XElement(parent.Name.Namespace + ADMTags.contentTag,
                new XAttribute(ADMTags.contentIDAttribute, ID),
                new XAttribute(ADMTags.contentNameAttribute, Name));
            parent.Add(root);
            foreach (ADMObject obj in Objects) {
                root.Add(new XElement(parent.Name.Namespace + ADMTags.objectRefTag, obj.ID));
                obj.Serialize(parent);
            }
            root.Add(new XElement(parent.Name.Namespace + ADMTags.contentDialogueTag,
                (int)ADMDialogueType.mixedContentKind,
                new XAttribute(ADMDialogueType.mixedContentKind.ToString(), (int)MixedContentKind.Undefined)));
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.contentIDAttribute);
            Name = source.GetAttribute(ADMTags.contentNameAttribute);
            // TODO: object references
        }
    }
}