using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Exceptions;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// Writes the root YAML file for Dolby Atmos Master Format exports.
    /// </summary>
    public class DolbyAtmosMasterFormatRootFile {
        /// <summary>
        /// Number of channels mapped to <see cref="bedIDs"/>.
        /// </summary>
        public int BedChannelCount => bedIDs.Length;

        /// <summary>
        /// Original static bed channel sources.
        /// </summary>
        readonly StaticSource[] staticObjects;

        /// <summary>
        /// Assigned bed channel IDs.
        /// </summary>
        readonly int[] bedIDs;

        /// <summary>
        /// Parse bed channel IDs from the provided static objects.
        /// </summary>
        /// <param name="staticObjects">Original static bed channel sources</param>
        public DolbyAtmosMasterFormatRootFile(StaticSource[] staticObjects) {
            this.staticObjects = staticObjects;
            bedIDs = staticObjects
                .Select(x => GetBedChannelID(x.Channel))
                .TakeWhile(x => x != -1)
                .ToArray();
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
        /// Write the root YAML file containing presentation, bed, and object definitions.
        /// </summary>
        /// <param name="writer">The stream to write the root file to</param>
        /// <param name="sourceCount">Total number of active sources</param>
        /// <param name="channelIDs">Array to populate with assigned channel IDs</param>
        public void Write(StreamWriter writer, int sourceCount, int[] channelIDs) {
            string rootFile = Path.GetFileName(((FileStream)writer.BaseStream).Name);
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
                    channelIDs[i] = bedIDs[i];
                }
            }

            int objectCount = sourceCount - bedChannels;
            if (objectCount == 0) {
                writer.WriteLine("    objects: []");
            } else {
                writer.WriteLine("    objects:");
                for (int i = 0; i < objectCount; i++) {
                    writer.WriteLine("      - ID: " + (10 + i));
                    channelIDs[i + bedChannels] = 10 + i;
                }
            }
        }
    }
}
