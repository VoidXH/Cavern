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
    public sealed class ADMChannelFormat : TaggedADMElement {
        /// <summary>
        /// Type of the contained tracks (channels, objects, etc.).
        /// </summary>
        public ADMPackType Type { get; private set; }

        /// <summary>
        /// Positional data for each timeslot.
        /// </summary>
        public List<ADMBlockFormat> Blocks { get; set; }

        /// <summary>
        /// Positional data of a channel/object.
        /// </summary>
        public ADMChannelFormat(string id, string name, ADMPackType type) {
            ID = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Constructs a channel format from an XML element.
        /// </summary>
        public ADMChannelFormat(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override XElement Serialize(XNamespace ns) {
            XElement root = new XElement(ns + ADMTags.channelFormatTag,
                new XAttribute(ADMTags.channelFormatIDAttribute, ID),
                new XAttribute(ADMTags.channelFormatNameAttribute, Name),
                new XAttribute(ADMTags.typeStringAttribute, Type),
                new XAttribute(ADMTags.typeAttribute, ((int)Type).ToString("x4")));
            string namePrefix = $"AB_{ID[3..]}_";
            int index = 0;
            foreach (ADMBlockFormat block in Blocks) {
                var newBlock = new XElement(ns + ADMTags.blockTag);
                newBlock.Add(new XAttribute(ADMTags.blockIDAttribute, namePrefix + (++index).ToString("x8")),
                    new XAttribute(ADMTags.blockOffsetAttribute, block.Offset.GetTimestamp()),
                    new XAttribute(ADMTags.durationAttribute, block.Duration.GetTimestamp()),
                    new XElement(ns + ADMTags.blockCartesianTag, 1),
                    new XElement(ns + ADMTags.blockPositionTag, block.Position.X,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'X')),
                    new XElement(ns + ADMTags.blockPositionTag, block.Position.Z,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'Y')));
                if (block.Position.Y != 0) {
                    newBlock.Add(new XElement(ns + ADMTags.blockPositionTag, block.Position.Y,
                        new XAttribute(ADMTags.blockCoordinateAttribute, 'Z')));
                }
                newBlock.Add(new XElement(ns + ADMTags.blockJumpTag, 1,
                    new XAttribute(ADMTags.blockJumpLengthAttribute,
                        block.Interpolation.TotalSeconds.ToString("0.000000").Replace(',', '.'))));
                root.Add(newBlock);
            }
            return root;
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.channelFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.channelFormatNameAttribute);
            Type = (ADMPackType)int.Parse(source.GetAttribute(ADMTags.typeAttribute));
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