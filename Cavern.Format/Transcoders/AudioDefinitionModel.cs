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
        public IReadOnlyList<ADMProgramme> Programs { get; private set; }

        /// <summary>
        /// Positional data for all channels/objects.
        /// </summary>
        public IReadOnlyList<ADMChannelFormat> Movements => movements;
        readonly List<ADMChannelFormat> movements = new List<ADMChannelFormat>();

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
        /// Creates an ADM for export by a program list created in code.
        /// </summary>
        public AudioDefinitionModel(List<ADMProgramme> programs, int sampleRate) {
            Programs = programs;
            this.sampleRate = sampleRate;
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
            XNamespace xmlns = XNamespace.Get(ADMTags.rootNamespace);
            XNamespace xsi = XNamespace.Get(ADMTags.instanceNamespace);
            XElement root = new XElement(xmlns + ADMTags.rootTag,
                new XAttribute(XNamespace.Xmlns + ADMTags.instanceNamespaceAttribute, xsi),
                new XAttribute(xsi + ADMTags.schemaLocationAttribute, ADMTags.rootNamespace + ADMTags.schemaLocation),
                new XAttribute(XNamespace.Xml + ADMTags.languageAttribute, ADMTags.language));
            XDocument doc = new XDocument(root);
            for (int i = 0; i < ADMTags.subTags.Length; i++) {
                XElement subTag = new XElement(xmlns + ADMTags.subTags[i]);
                root.Add(subTag);
                root = subTag;
            }
            foreach (ADMProgramme program in Programs) {
                program.Serialize(root);
            }
            doc.WriteTo(writer);
        }

        public XmlSchema GetSchema() => null;

        /// <summary>
        /// Read all programs from an XML file.
        /// </summary>
        void ParsePrograms(XDocument data) {
            List<ADMProgramme> result = new List<ADMProgramme>();
            IEnumerable<XElement> programs = data.AllDescendants(ADMTags.programTag);
            foreach (XElement program in programs) {
                result.Add(new ADMProgramme(program.GetAttribute(ADMTags.programIDAttribute),
                    program.GetAttribute(ADMTags.programNameAttribute), 0) {
                    Contents = ParseContents(data, program)
                });
            }
            Programs = result;
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
                result.Add(new ADMObject(obj.Value, objectElement.GetAttribute(ADMTags.objectNameAttribute),
                    ParseTimestamp(obj.Attribute(ADMTags.startAttribute)),
                    ParseTimestamp(obj.Attribute(ADMTags.durationAttribute))) {
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
                ChannelFormats = ParseChannelFormats(data, node)
            };
        }

        /// <summary>
        /// Read the pack format of an object.
        /// </summary>
        List<ADMChannelFormat> ParseChannelFormats(XDocument data, XElement pack) {
            List<ADMChannelFormat> result = new List<ADMChannelFormat>();
            IEnumerable<XElement> channels = pack.AllDescendants(ADMTags.channelFormatRefTag);
            foreach (XElement channel in channels) {
                XElement channelElement =
                    data.GetWithAttribute(ADMTags.channelFormatTag, ADMTags.channelFormatIDAttribute, channel.Value);
                ADMChannelFormat parsed = new ADMChannelFormat() {
                    ID = channel.Value,
                    Name = channelElement.GetAttribute(ADMTags.channelFormatNameAttribute),
                    Blocks = ParseBlockFormats(channelElement)
                };
                result.Add(parsed);
                movements.Add(parsed);
            }
            return result;
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
                long duration = ParseTimestamp(block.Attribute(ADMTags.durationAttribute)),
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