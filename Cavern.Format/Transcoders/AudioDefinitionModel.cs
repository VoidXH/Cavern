using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using Cavern.Format.Common;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// An XML file with channel and object information.
    /// </summary>
    public class AudioDefinitionModel : IXmlSerializable {
        /// <summary>
        /// Programs contained in the ADM descriptor.
        /// </summary>
        public List<ADMProgramme> Programs { get; set; }

        /// <summary>
        /// Sample rate of the described content.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Parses an XML file with channel and object information.
        /// </summary>
        public AudioDefinitionModel(Stream reader, int length, int sampleRate) {
            this.sampleRate = sampleRate;
            byte[] data = new byte[length];
            reader.Read(data, 0, length);
            using XmlReader xmlReader = XmlReader.Create(new MemoryStream(data));
            ReadXml(xmlReader);
        }

        /// <summary>
        /// Extracts the ADM metadata from an XML file.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader) => ParsePrograms(XDocument.Load(reader));

        /// <summary>
        /// Writes the ADM metadata to an XML file.
        /// </summary>
        public void WriteXml(XmlWriter writer) {
            throw new System.NotImplementedException();
        }

        public XmlSchema GetSchema() => null;

        /// <summary>
        /// Read all programs from an XML file.
        /// </summary>
        void ParsePrograms(XDocument data) {
            Programs = new List<ADMProgramme>();
            IEnumerable<XElement> programs = data.AllDescendants(ADMTags.programTag);
            foreach (XElement program in programs) {
                Programs.Add(new ADMProgramme() {
                    ID = program.GetAttribute(ADMTags.programIDAttribute),
                    Name = program.GetAttribute(ADMTags.programNameAttribute),
                    Contents = ParseContents(data, program)
                });
            }
        }

        /// <summary>
        /// Read all contents for a single program.
        /// </summary>
        List<ADMContent> ParseContents(XDocument data, XElement program) {
            List<ADMContent> result = new List<ADMContent>();
            IEnumerable<XElement> contents = program.AllDescendants(ADMTags.contentRefTag);
            foreach (XElement content in contents) {
                XElement contentElement = data.GetWithAttribute(ADMTags.contentTag, ADMTags.contentIDAttribute, content.Value);
                result.Add(new ADMContent() {
                    ID = content.Value,
                    Name = contentElement.GetAttribute(ADMTags.contentNameAttribute),
                    Objects = ParseObjects(data, contentElement)
                });
            }
            return result;
        }

        /// <summary>
        /// Read all objects for a single content.
        /// </summary>
        List<ADMObject> ParseObjects(XDocument data, XElement content) {
            List<ADMObject> result = new List<ADMObject>();
            IEnumerable<XElement> objects = content.AllDescendants(ADMTags.objectRefTag);
            foreach (XElement obj in objects) {
                XElement objectElement = data.GetWithAttribute(ADMTags.objectTag, ADMTags.objectIDAttribute, obj.Value);
                result.Add(new ADMObject() {
                    ID = obj.Value,
                    Name = objectElement.GetAttribute(ADMTags.objectNameAttribute),
                    PackFormat = ParsePackFormat(data, objectElement)
                });
            }
            return result;
        }

        /// <summary>
        /// Read the pack format of an object.
        /// </summary>
        ADMPackFormat ParsePackFormat(XDocument data, XElement parent) {
            IEnumerable<XElement> refs = parent.AllDescendants(ADMTags.packFormatRefTag);
            using IEnumerator<XElement> pack = refs.GetEnumerator();
            if (!pack.MoveNext()) {
                return null;
            }
            XElement node = data.GetWithAttribute(ADMTags.packFormatTag, ADMTags.packFormatIDAttribute, pack.Current.Value);
            return new ADMPackFormat() {
                ID = pack.Current.Value,
                Name = node.GetAttribute(ADMTags.packFormatNameAttribute),
                Type = (ADMPackType)int.Parse(node.GetAttribute(ADMTags.packFormatTypeAttribute)),
                ChannelFormat = ParseChannelFormat(data, node)
            };
        }

        /// <summary>
        /// Read the pack format of an object.
        /// </summary>
        ADMChannelFormat ParseChannelFormat(XDocument data, XElement parent) {
            IEnumerable<XElement> refs = parent.AllDescendants(ADMTags.channelFormatRefTag);
            using IEnumerator<XElement> format = refs.GetEnumerator();
            if (!format.MoveNext()) {
                return null;
            }
            XElement node =
                data.GetWithAttribute(ADMTags.channelFormatTag, ADMTags.channelFormatIDAttribute, format.Current.Value);
            return new ADMChannelFormat() {
                ID = format.Current.Value,
                Name = node.GetAttribute(ADMTags.channelFormatNameAttribute),
                Blocks = ParseBlockFormats(node)
            };
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
                long duration = ParseTimestamp(block.Attribute(ADMTags.blockDurationAttribute)),
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
                                    y = value;
                                    break;
                                case 'Z':
                                    z = value;
                                    break;
                                default:
                                    throw new CorruptionException(block.GetAttribute(ADMTags.blockIDAttribute));
                            }
                            break;
                        case ADMTags.blockJumpTag:
                            if (child.Value[0] == '1') {
                                XAttribute length = child.Attribute(ADMTags.blockJumpLengthAttribute);
                                interpolation = length != null ? (long)(QMath.ParseFloat(length.Value) * sampleRate) : 0;
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
        /// Convert a timestamp to samples if its attribute is present.
        /// </summary>
        long ParseTimestamp(XAttribute attribute) => attribute != null ?
            (long)(TimeSpan.Parse(attribute.Value).TotalSeconds * sampleRate) : 0;
    }
}