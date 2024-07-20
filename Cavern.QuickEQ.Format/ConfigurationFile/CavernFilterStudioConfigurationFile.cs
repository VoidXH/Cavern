using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile.Helpers;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Cavern Filter Studio's own export format for full grouped filter pipelines.
    /// </summary>
    public sealed class CavernFilterStudioConfigurationFile : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => "cfs";

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Cavern's format.
        /// </summary>
        public CavernFilterStudioConfigurationFile(ConfigurationFile other) : base(other) { }

        /// <summary>
        /// Import a Cavern Filter Studio configuration file from a <paramref name="path"/>.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string path) : base(ParseSplitPoints(path)) => FinishLazySetup(131072);

        /// <summary>
        /// Create an empty file for a standard layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, int channelCount) :
            this(name, ChannelPrototype.GetStandardMatrix(channelCount)) { }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, params ReferenceChannel[] channels) : base(name, channels) =>
            FinishEmpty(channels);

        /// <summary>
        /// Import a Cavern Filter Studio configuration file from a <paramref name="path"/>.
        /// </summary>
        static List<(string, FilterGraphNode[])> ParseSplitPoints(string path) {
            using XmlReader reader = XmlReader.Create(path);
            int index = -1;
            List<FilterGraphNode> nodes = new List<FilterGraphNode>();
            List<(string name, FilterGraphNode[] roots)> splitPoints = new List<(string name, FilterGraphNode[] roots)>();
            while (reader.Read()) {
                if (reader.NodeType != XmlNodeType.Element || reader.Name == nameof(CavernFilterStudioConfigurationFile)) {
                    continue;
                }

                if (reader.Name == nameof(FilterGraphNode)) {
                    string parentsSource = null;
                    while (reader.MoveToNextAttribute()) {
                        switch (reader.Name) {
                            case indexAttribute:
                                index = int.Parse(reader.Value);
                                break;
                            case parentsAttribute:
                                parentsSource = reader.Value;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    FilterGraphNode node = new FilterGraphNode(null);
                    if (!string.IsNullOrEmpty(parentsSource)) {
                        string[] parents = parentsSource.Split(',');
                        for (int i = 0; i < parents.Length; i++) {
                            node.AddParent(nodes[int.Parse(parents[i])]);
                        }
                    }
                    nodes.Add(node);
                } else if (reader.Name == splitPointElement) {
                    string name = null,
                        rootsSource = null;
                    while (reader.MoveToNextAttribute()) {
                        switch (reader.Name) {
                            case nameAttribute:
                                name = reader.Value;
                                break;
                            case rootsAttribute:
                                rootsSource = reader.Value;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    splitPoints.Add((name, rootsSource.Split(',').Select(x => nodes[int.Parse(x)]).ToArray()));
                } else {
                    nodes[index].Filter = ParseFilter(reader);
                }
            }
            return splitPoints;
        }

        /// <summary>
        /// Parse a filter from a <see cref="CavernFilterStudioConfigurationFile"/>, including the ones not in the base Cavern library.
        /// </summary>
        static Filter ParseFilter(XmlReader reader) {
            switch (reader.Name) {
                case nameof(GraphicEQ):
                    LazyGraphicEQ graphicEQ = new LazyGraphicEQ(new Equalizer(), Listener.DefaultSampleRate);
                    graphicEQ.ReadXml(reader);
                    return graphicEQ;
                case nameof(InputChannel):
                    InputChannel inputChannel = new InputChannel(ReferenceChannel.Unknown);
                    inputChannel.ReadXml(reader);
                    return inputChannel;
                case nameof(OutputChannel):
                    OutputChannel outputChannel = new OutputChannel(ReferenceChannel.Unknown);
                    outputChannel.ReadXml(reader);
                    return outputChannel;
                default:
                    return Filter.FromXml(reader);
            }
        }

        /// <summary>
        /// Throw a <see cref="NotCavernFilterStudioFilterException"/> if a filter couldn't be exported.
        /// </summary>
        static void ValidateForExport((FilterGraphNode node, int _)[] exportOrder) {
            for (int i = 0; i < exportOrder.Length; i++) {
                Filter filter = exportOrder[i].node.Filter;
                if (filter != null && !(filter is IXmlSerializable)) {
                    throw new NotCavernFilterStudioFilterException(filter);
                }
            }
        }

        /// <inheritdoc/>
        public override void Export(string path) {
            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            ValidateForExport(exportOrder);

            XmlWriterSettings settings = new XmlWriterSettings {
                Indent = true
            };
            using XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartElement(nameof(CavernFilterStudioConfigurationFile));
            for (int i = 0; i < exportOrder.Length; i++) {
                writer.WriteStartElement(nameof(FilterGraphNode));
                writer.WriteAttributeString(indexAttribute, i.ToString());
                string parents = string.Join(',', GetExportedParentIndices(exportOrder, i));
                if (parents.Length != 0) {
                    writer.WriteAttributeString(parentsAttribute, parents);
                }
                Filter filter = exportOrder[i].node.Filter;
                ((IXmlSerializable)filter).WriteXml(writer);
                writer.WriteEndElement();
            }
            for (int i = 0, c = SplitPoints.Count; i < c; i++) {
                writer.WriteStartElement(splitPointElement);
                writer.WriteAttributeString(nameAttribute, SplitPoints[i].name);
                writer.WriteAttributeString(rootsAttribute, string.Join(',', SplitPoints[i].roots.Select(x => {
                    for (int j = 0; j < exportOrder.Length; j++) {
                        if (exportOrder[j].node == x) {
                            return j;
                        }
                    }
                    throw new DataMisalignedException();
                })));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// XML attribute of <see cref="FilterGraphNode"/> indices.
        /// </summary>
        const string indexAttribute = "Index";

        /// <summary>
        /// XML attribute of indices of a <see cref="FilterGraphNode"/>'s parents.
        /// </summary>
        const string parentsAttribute = "Parents";

        /// <summary>
        /// XML element representing one of the <see cref="ConfigurationFile.SplitPoints"/>.
        /// </summary>
        const string splitPointElement = "SplitPoint";

        /// <summary>
        /// Name of one of the <see cref="ConfigurationFile.SplitPoints"/>.
        /// </summary>
        const string nameAttribute = "Name";

        /// <summary>
        /// Indices of root elements in a split point.
        /// </summary>
        const string rootsAttribute = "Roots";
    }
}