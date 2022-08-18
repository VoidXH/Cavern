using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Positional data of a channel/object.
    /// </summary>
    public class ADMChannelFormat : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// The parent pack format.
        /// </summary>
        public ADMPackFormat PackFormat { get; set; }

        /// <summary>
        /// Positional data for each timeslot.
        /// </summary>
        public List<ADMBlockFormat> Blocks { get; set; }

        /// <summary>
        /// Positional data of a channel/object.
        /// </summary>
        public ADMChannelFormat(string id, string name, ADMPackFormat packFormat) {
            ID = id;
            Name = name;
            PackFormat = packFormat;
        }

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            XElement root = new XElement(parent.Name.Namespace + ADMTags.channelFormatTag,
                new XAttribute(ADMTags.channelFormatIDAttribute, ID),
                new XAttribute(ADMTags.channelFormatNameAttribute, Name),
                new XAttribute(ADMTags.typeStringAttribute, PackFormat.Type),
                new XAttribute(ADMTags.typeAttribute, ((int)PackFormat.Type).ToString("x4")));
            parent.Add(root);
            string namePrefix = $"AB_{ID[3..]}_";
            int index = 0;
            double samplesToTime = 1.0 / PackFormat.Object.Track.SampleRate;
            foreach (ADMBlockFormat block in Blocks) {
                var newBlock = new XElement(parent.Name.Namespace + ADMTags.blockTag);
                newBlock.Add(new XAttribute(ADMTags.blockIDAttribute, namePrefix + (++index).ToString("x8")),
                    new XAttribute(ADMTags.blockOffsetAttribute,
                        TimeSpan.FromSeconds(block.Offset * samplesToTime).GetTimestamp()),
                    new XAttribute(ADMTags.durationAttribute,
                        TimeSpan.FromSeconds(block.Duration * samplesToTime).GetTimestamp()),
                    new XElement(parent.Name.Namespace + ADMTags.blockCartesianTag, 1),
                    new XElement(parent.Name.Namespace + ADMTags.blockPositionTag, block.Position.X,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'X')),
                    new XElement(parent.Name.Namespace + ADMTags.blockPositionTag, block.Position.Z,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'Y')));
                if (block.Position.Y != 0) {
                    newBlock.Add(new XElement(parent.Name.Namespace + ADMTags.blockPositionTag, block.Position.Y,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'Z')));
                }
                newBlock.Add(new XElement(parent.Name.Namespace + ADMTags.blockJumpTag, 1,
                    new XAttribute(ADMTags.blockJumpLengthAttribute,
                        (block.Interpolation * samplesToTime).ToString("0.000000").Replace(',', '.'))));
                root.Add(newBlock);
            }
        }
    }
}