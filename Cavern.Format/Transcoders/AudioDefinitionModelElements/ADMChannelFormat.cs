using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Positional data of a channel/object.
    /// </summary>
    public sealed class ADMChannelFormat : TaggedADMElement, IXDocumentSerializable {
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
        /// Constructs a channel format from an XML element.
        /// </summary>
        public ADMChannelFormat(XElement source) => Deserialize(source);

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
            double samplesToTime = 1.0 / PackFormat.Object.Tracks[0].SampleRate;
            foreach (ADMBlockFormat block in Blocks) {
                var newBlock = new XElement(parent.Name.Namespace + ADMTags.blockTag);
                newBlock.Add(new XAttribute(ADMTags.blockIDAttribute, namePrefix + (++index).ToString("x8")),
                    new XAttribute(ADMTags.blockOffsetAttribute, block.Offset.GetTimestamp()),
                    new XAttribute(ADMTags.durationAttribute, block.Duration.GetTimestamp()),
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

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.channelFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.channelFormatNameAttribute);
            Blocks = ParseBlockFormats(source);
        }

        /// <summary>
        /// Read the movement of an object.
        /// </summary>
        List<ADMBlockFormat> ParseBlockFormats(XElement channel) {
            List<ADMBlockFormat> result = new List<ADMBlockFormat>();
            IEnumerable<XElement> blocks = channel.AllDescendants(ADMTags.blockTag);
            foreach (XElement block in blocks) {
                bool cartesian = false;
                float x = 0, y = 0, z = 0;
                TimeSpan duration = ParseTimestamp(block.Attribute(ADMTags.durationAttribute)),
                    interpolation = duration;
                IEnumerable<XElement> children = block.Descendants();
                foreach (XElement child in children) {
                    switch (child.Name.LocalName) {
                        case ADMTags.blockCartesianTag:
                            cartesian = child.Value[0] == '1';
                            break;
                        case ADMTags.blockPositionTag:
                            float value = QMath.ParseFloat(child.Value);
                            switch (child.GetAttribute(ADMTags.blockCoordinateAttribute)[0]) {
                                case 'X':
                                    x = value;
                                    break;
                                case 'Y':
                                    z = value;
                                    break;
                                case 'Z':
                                    y = value;
                                    break;
                                default:
                                    throw new CorruptionException(block.GetAttribute(ADMTags.blockIDAttribute));
                            }
                            break;
                        case ADMTags.blockJumpTag:
                            if (child.Value[0] == '1') {
                                XAttribute length = child.Attribute(ADMTags.blockJumpLengthAttribute);
                                interpolation = length != null ? TimeSpan.FromSeconds(QMath.ParseFloat(length.Value)) : default;
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (!cartesian) {
                    throw new UnsupportedFeatureException("polar");
                }
                result.Add(new ADMBlockFormat() {
                    Offset = ParseTimestamp(block.Attribute(ADMTags.blockOffsetAttribute)),
                    Duration = duration,
                    Position = new Vector3(x, y, z),
                    Interpolation = interpolation
                });
            }
            return result;
        }
    }
}