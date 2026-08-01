using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Exceptions;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// The root YAML file for Dolby Atmos Master Format presentations, including basic metadata and the channel/object mapping.
    /// </summary>
    public class DolbyAtmosMasterFormatRootFile : IExportable {
        /// <summary>
        /// Root file name used when exporting to a stream without a file path.
        /// </summary>
        const string MemoryRootFileName = "memory.atmos";

        /// <summary>
        /// Bed channels parsed from the root file or exported to it.
        /// </summary>
        public ReferenceChannel[] Channels { get; }

        /// <summary>
        /// Maps PCM stream indices from the .audio file to internal object ID (those are the values).
        /// </summary>
        public int[] ObjectMapping { get; }

        /// <summary>
        /// Number of channels mapped to <see cref="bedIDs"/>.
        /// </summary>
        public int BedChannelCount => bedIDs.Length;

        /// <inheritdoc/>
        public string FileExtension => "atmos";

        /// <summary>
        /// Original static bed channel sources.
        /// </summary>
        readonly StaticSource[] staticObjects;

        /// <summary>
        /// Assigned bed channel IDs.
        /// </summary>
        readonly int[] bedIDs;

        /// <summary>
        /// Number of object channels.
        /// </summary>
        readonly int objectCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="DolbyAtmosMasterFormatRootFile"/> class.
        /// </summary>
        /// <param name="staticObjects">Sources with fixed reference positions</param>
        /// <param name="sources">Active sources in the same order the <see cref="Listener"/> iterates them (<see cref="Listener.ActiveSources"/>)</param>
        /// <param name="channelIDs">Array to populate with assigned channel IDs</param>
        public DolbyAtmosMasterFormatRootFile(StaticSource[] staticObjects, IReadOnlyList<Source> sources, int[] channelIDs) {
            this.staticObjects = staticObjects;
            bedIDs = staticObjects
                .Select(x => GetBedChannelID(x.Channel))
                .TakeWhile(x => x != -1)
                .ToArray();
            Channels = staticObjects
                .Take(bedIDs.Length)
                .Select(x => x.Channel)
                .ToArray();
            ObjectMapping = channelIDs;

            Dictionary<Source, int> sourceToBedIndex = new Dictionary<Source, int>();
            for (int i = 0; i < staticObjects.Length; i++) {
                sourceToBedIndex[staticObjects[i].Source] = i;
            }

            objectCount = 0;
            for (int i = 0; i < sources.Count; i++) {
                Source source = sources[i];
                if (sourceToBedIndex.TryGetValue(source, out int bedIndex)) {
                    channelIDs[i] = bedIDs[bedIndex];
                } else {
                    channelIDs[i] = 10 + objectCount++;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DolbyAtmosMasterFormatRootFile"/> class for parsing.
        /// </summary>
        /// <param name="source">Pre-parsed root YAML contents</param>
        /// <param name="channelCount">Number of PCM channels in the corresponding audio file</param>
        public DolbyAtmosMasterFormatRootFile(string source, int channelCount) : this(new YAML(source), channelCount) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DolbyAtmosMasterFormatRootFile"/> class for parsing.
        /// </summary>
        /// <param name="source">Pre-parsed root YAML file</param>
        /// <param name="channelCount">Number of PCM channels in the corresponding audio file</param>
        internal DolbyAtmosMasterFormatRootFile(YAML source, int channelCount) {
            ObjectMapping = new int[channelCount];

            if (!(source.Data.TryGetValue("presentations", out object rawPresentations) &&
                rawPresentations is List<YAMLObject> presentations &&
                presentations.Count == 1)) {
                throw new CorruptionException(nameof(rawPresentations));
            }
            YAMLObject presentation = presentations[0];
            if (!(presentation.TryGetValue("bedInstances", out object rawBedInstances) &&
                rawBedInstances is List<YAMLObject> bedInstances &&
                bedInstances.Count == 1 &&
                bedInstances[0] is YAMLObject bedInstance &&
                bedInstance.TryGetValue("channels", out object rawChannels))) {
                throw new CorruptionException(nameof(rawBedInstances));
            }

            if (rawChannels is List<YAMLObject> channelsSource) {
                if (channelsSource.Count > channelCount) {
                    throw new CorruptionException(nameof(channelsSource));
                }
                Channels = ParseChannels(channelsSource);
            } else {
                Channels = Array.Empty<ReferenceChannel>();
            }

            int expectedObjectCount = channelCount - Channels.Length;
            objectCount = ParseObjectCount(presentation, expectedObjectCount);
            for (int i = 0; i < objectCount; i++) {
                ObjectMapping[Channels.Length + i] = 10 + i;
            }

            staticObjects = Channels
                .Select(channel => new StaticSource(channel, null))
                .ToArray();
            bedIDs = ObjectMapping.Take(Channels.Length).ToArray();
        }

        /// <summary>
        /// Translate <see cref="ReferenceChannel"/>s to Dolby Atmos bed channel IDs.
        /// </summary>
        static int GetBedChannelID(ReferenceChannel channel) {
            if (channel <= ReferenceChannel.SideRight) {
                return (int)channel;
            } else if (channel == ReferenceChannel.TopSideLeft) {
                return 8;
            } else if (channel == ReferenceChannel.TopSideRight) {
                return 9;
            } else {
                if (CavernFormatGlobal.Unsafe) {
                    return -1;
                } else {
                    throw new InvalidExportChannelException(true, channel);
                }
            }
        }

        /// <summary>
        /// Determine the object count from the root file.
        /// </summary>
        static int ParseObjectCount(YAMLObject presentation, int expectedObjectCount) {
            if (!presentation.TryGetValue("objects", out object rawObjects)) {
                if (expectedObjectCount == 0) {
                    return 0;
                }
                throw new CorruptionException(nameof(presentation));
            }

            if (rawObjects is string rawObjectText) {
                if (rawObjectText == "[]") {
                    if (expectedObjectCount == 0) {
                        return 0;
                    }
                    throw new CorruptionException(nameof(rawObjectText));
                }

                throw new CorruptionException(nameof(rawObjects));
            }

            if (!(rawObjects is List<YAMLObject> objects)) {
                throw new CorruptionException(nameof(objects));
            }

            if (objects.Count != expectedObjectCount) {
                throw new CorruptionException($"{nameof(objects)}.Count");
            }

            return objects.Count;
        }

        /// <inheritdoc/>
        public void Export(Stream stream) {
            using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            string rootFile = stream is FileStream fileStream ? Path.GetFileName(fileStream.Name) : MemoryRootFileName;
            int bedChannels = bedIDs.Length;

            writer.WriteLine("version: 0.5.1");
            writer.WriteLine("presentations:");
            writer.WriteLine("  - type: home");
            writer.WriteLine("    simplified: false");
            writer.WriteLine($"    metadata: {rootFile}.metadata");
            writer.WriteLine($"    audio: {rootFile}.audio");
            writer.WriteLine("    offset: 0.0");
            writer.WriteLine("    fps: 24");
            writer.WriteLine($"    scBedConfiguration: [{string.Join(", ", bedIDs)}]");
            writer.WriteLine("    creationTool: Cavern");
            writer.WriteLine("    creationToolVersion: " + Listener.Version);
            writer.WriteLine("    bedInstances:");

            if (bedChannels == 0) {
                writer.WriteLine("      - channels: []");
            } else {
                writer.WriteLine("      - channels:");
                for (int i = 0; i < bedChannels; i++) {
                    writer.WriteLine("          - channel: " + staticObjects[i].Channel.GetShortNameDCI());
                    writer.WriteLine("            ID: " + bedIDs[i]);
                }
            }

            if (objectCount == 0) {
                writer.WriteLine("    objects: []");
            } else {
                writer.WriteLine("    objects:");
                for (int i = 0; i < objectCount; i++) {
                    writer.WriteLine("      - ID: " + (10 + i));
                }
            }
            writer.Flush();
        }

        /// <inheritdoc/>
        public void Export(string path) {
            using FileStream stream = File.OpenWrite(path);
            Export(stream);
        }

        /// <summary>
        /// Convert the bed channels to <see cref="ReferenceChannel"/>s.
        /// </summary>
        ReferenceChannel[] ParseChannels(List<YAMLObject> channels) {
            ReferenceChannel[] result = new ReferenceChannel[channels.Count];
            for (int i = 0; i < result.Length; i++) {
                if (!(channels[i] is YAMLObject channel &&
                    channel.TryGetValue("channel", out object rawName) &&
                    rawName is string name &&
                    channel.TryGetValue("ID", out object rawId) &&
                    rawId is string idString &&
                    int.TryParse(idString, out int id))) {
                    throw new CorruptionException(nameof(channels));
                }

                result[i] = ChannelPrototype.FromStandardName(name);
                if (result[i] == ReferenceChannel.Unknown) {
                    throw new CorruptionException($"{nameof(result)}[{i}] = {name}");
                }
                ObjectMapping[i] = id;
            }
            return result;
        }
    }
}
