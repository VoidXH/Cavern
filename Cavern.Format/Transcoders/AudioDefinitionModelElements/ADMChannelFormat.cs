using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

using Cavern.Format.Common;
using Cavern.Format.Renderers;
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
        public override void Serialize(XmlWriter writer) {
            writer.WriteStartElement(ADMTags.channelFormatTag);
            writer.WriteAttributeString(ADMTags.channelFormatIDAttribute, ID);
            writer.WriteAttributeString(ADMTags.channelFormatNameAttribute, Name);
            writer.WriteAttributeString(ADMTags.typeStringAttribute, Type.ToString());
            writer.WriteAttributeString(ADMTags.typeAttribute, ((int)Type).ToString("x4"));
            string namePrefix = $"AB_{ID[3..]}_";

            if (Type == ADMPackType.Objects || Blocks.Count != 1) {
                int index = 0;
                foreach (ADMBlockFormat block in Blocks) {
                    SerializeBlock(writer, block, namePrefix, ++index);
                }
            } else {
                SerializeOnlyBlock(writer, Blocks[0], namePrefix, 1);
            }

            writer.WriteEndElement();
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
        /// Serialization of a single position update block.
        /// </summary>
        void SerializeBlock(XmlWriter writer, ADMBlockFormat block, string namePrefix, int index) {
            writer.WriteStartElement(ADMTags.blockTag);
            SerializeBlockMain(writer, block, namePrefix, index);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Serialization of the only position update block.
        /// </summary>
        void SerializeOnlyBlock(XmlWriter writer, ADMBlockFormat block, string namePrefix, int index) {
            writer.WriteStartElement(ADMTags.blockTag);
            SerializeBlockMain(writer, block, namePrefix, index);
            writer.WriteElementString(ADMTags.blockLabelAttribute,
                ADMConsts.channelLabels[(int)Renderer.ChannelFromPosition(Blocks[0].Position)]);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Inner serialization of a single block element without element start/end to be extensible.
        /// </summary>
        void SerializeBlockMain(XmlWriter writer, ADMBlockFormat block, string namePrefix, int index) {
            writer.WriteAttributeString(ADMTags.blockIDAttribute, namePrefix + index.ToString("x8"));
            writer.WriteAttributeString(ADMTags.blockOffsetAttribute, block.Offset.GetTimestamp());
            writer.WriteAttributeString(ADMTags.durationAttribute, block.Duration.GetTimestamp());
            writer.WriteElementString(ADMTags.blockCartesianTag, enabledValue);
            writer.WriteStartElement(ADMTags.blockPositionTag);
            writer.WriteAttributeString(ADMTags.blockCoordinateAttribute, xAxis);
            writer.WriteString(block.Position.X.ToString().Replace(',', '.'));
            writer.WriteEndElement();
            writer.WriteStartElement(ADMTags.blockPositionTag);
            writer.WriteAttributeString(ADMTags.blockCoordinateAttribute, yAxis);
            writer.WriteString(block.Position.Z.ToString().Replace(',', '.'));
            writer.WriteEndElement();
            if (block.Position.Y != 0) {
                writer.WriteStartElement(ADMTags.blockPositionTag);
                writer.WriteAttributeString(ADMTags.blockCoordinateAttribute, zAxis);
                writer.WriteString(block.Position.Y.ToString().Replace(',', '.'));
                writer.WriteEndElement();
            }
            writer.WriteStartElement(ADMTags.blockJumpTag);
            writer.WriteAttributeString(ADMTags.blockJumpLengthAttribute,
                block.Interpolation.TotalSeconds.ToString("0.000000").Replace(',', '.'));
            writer.WriteString(enabledValue);
            writer.WriteEndElement();
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

        /// <summary>
        /// Value that marks an enabled feature.
        /// </summary>
        const string enabledValue = "1";

        /// <summary>
        /// Value that marks the X axis position.
        /// </summary>
        const string xAxis = "X";

        /// <summary>
        /// Value that marks the X axis position.
        /// </summary>
        const string yAxis = "Y";

        /// <summary>
        /// Value that marks the X axis position.
        /// </summary>
        const string zAxis = "Z";
    }
}