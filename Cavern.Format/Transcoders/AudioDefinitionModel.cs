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
    public sealed class AudioDefinitionModel : IXmlSerializable {
        /// <summary>
        /// Complete presentations.
        /// </summary>
        public IReadOnlyList<ADMProgramme> Programs { get; private set; }

        /// <summary>
        /// Object groupings.
        /// </summary>
        public IReadOnlyList<ADMContent> Contents { get; private set; }

        /// <summary>
        /// Single/multitrack object roots.
        /// </summary>
        public IReadOnlyList<ADMObject> Objects { get; private set; }

        /// <summary>
        /// Object categorizers.
        /// </summary>
        public IReadOnlyList<ADMPackFormat> PackFormats { get; private set; }

        /// <summary>
        /// Channel positions and object movements.
        /// </summary>
        public IReadOnlyList<ADMChannelFormat> ChannelFormats { get; private set; }

        /// <summary>
        /// Format information of each discrete audio source.
        /// </summary>
        public IReadOnlyList<ADMTrack> Tracks { get; private set; }

        /// <summary>
        /// Positional data for all channels/objects.
        /// </summary>
        public IReadOnlyList<ADMChannelFormat> Movements => movements;
        List<ADMChannelFormat> movements = new List<ADMChannelFormat>();

        /// <summary>
        /// Parses an XML file with channel and object information.
        /// </summary>
        /// <param name="reader">Stream to read the AXML from</param>
        /// <param name="length">Length of the AXML stream in bytes</param>
        public AudioDefinitionModel(Stream reader, long length) {
            byte[] data = new byte[length];
            reader.Read(data, 0, length);
            using XmlReader xmlReader = XmlReader.Create(new MemoryStream(data));
            ReadXml(xmlReader);
        }

        /// <summary>
        /// Creates an ADM for export by a program list created in code.
        /// </summary>
        public AudioDefinitionModel(IReadOnlyList<ADMProgramme> programs, IReadOnlyList<ADMContent> contents) {
            Programs = programs;
            Contents = contents;
            // TODO: all groups
        }

        /// <summary>
        /// Change object order to reference the BWF file's correct channels.
        /// </summary>
        // TODO: let's just keep this one as API until parsing only parses by groups
        public void Assign(ChannelAssignment chna) {
        }

        /// <summary>
        /// Extracts the ADM metadata from an XML file.
        /// </summary>
        // TODO: Iterate through the tags, group them to lists for later quick search, check existence before reading anything
        public void ReadXml(XmlReader reader) {
            List<ADMProgramme> programs = new List<ADMProgramme>();
            List<ADMContent> contents = new List<ADMContent>();
            List<ADMObject> objects = new List<ADMObject>();
            List<ADMPackFormat> packFormats = new List<ADMPackFormat>();
            List<ADMChannelFormat> channelFormats = new List<ADMChannelFormat>();
            List<ADMTrack> tracks = new List<ADMTrack>();
            Programs = programs;
            Contents = contents;
            Objects = objects;
            PackFormats = packFormats;
            ChannelFormats = channelFormats;
            Tracks = tracks;
            movements = channelFormats;

            XDocument doc = XDocument.Load(reader);
            IEnumerable<XElement> descendants = doc.Descendants();
            using IEnumerator<XElement> enumerator = descendants.GetEnumerator();
            while (enumerator.MoveNext()) {
                switch (enumerator.Current.Name.LocalName) {
                    case ADMTags.programTag:
                        programs.Add(new ADMProgramme(enumerator.Current));
                        break;
                    case ADMTags.contentTag:
                        contents.Add(new ADMContent(enumerator.Current));
                        break;
                    case ADMTags.objectTag:
                        objects.Add(new ADMObject(enumerator.Current));
                        break;
                    case ADMTags.packFormatTag:
                        packFormats.Add(new ADMPackFormat(enumerator.Current));
                        break;
                    case ADMTags.channelFormatTag:
                        channelFormats.Add(new ADMChannelFormat(enumerator.Current));
                        break;
                    case ADMTags.trackTag:
                        tracks.Add(new ADMTrack(enumerator.Current));
                        break;
                }
            }

            // TODO: read references as strings as they are only needed once, update export accordingly
        }

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
    }
}