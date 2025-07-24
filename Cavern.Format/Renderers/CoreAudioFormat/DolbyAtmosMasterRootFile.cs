using System;
using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Renderers.CoreAudioFormat {
    /// <summary>
    /// Parses the root file of a Dolby Atmos Master Format exports.
    /// </summary>
    internal class DolbyAtmosMasterRootFile {
        /// <summary>
        /// The first PCM tracks are these specific channels.
        /// </summary>
        public ReferenceChannel[] Channels { get; }

        /// <summary>
        /// Maps PCM stream indices from the .audio file to internal object ID (those are the values).
        /// </summary>
        public int[] ObjectMapping { get; }

        /// <summary>
        /// Parses the root file of a Dolby Atmos Master Format exports.
        /// </summary>
        public DolbyAtmosMasterRootFile(YAML source, int channelCount) {
            if (!(source.Data.TryGetValue("presentations", out object rawPresentations) &&
                rawPresentations is List<YAMLObject> presentations &&
                presentations.Count == 1)) {
                throw new CorruptionException("Only single-presentation files are supported.");
            }
            YAMLObject presentation = presentations[0];
            if (!(presentation.TryGetValue("bedInstances", out object rawBedInstances) &&
                rawBedInstances is List<YAMLObject> bedInstances &&
                bedInstances.Count == 1 &&
                bedInstances[0] is YAMLObject bedInstance &&
                bedInstance.TryGetValue("channels", out object rawChannels) &&
                rawChannels is List<YAMLObject> channelsSource)) {
                throw new CorruptionException("Only single-bed instance files with a single channel assignment are supported.");
            }
            ObjectMapping = new int[channelCount];
            Channels = ParseChannels(channelsSource);

            if (!(presentation.TryGetValue("objects", out object rawObjects) &&
                rawObjects is List<YAMLObject> objects)) {
                throw new CorruptionException("Object ID mapping was not found.");
            }
            ParseObjectIDs(objects, Channels.Length);
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
                    throw new CorruptionException("Invalid channel definition in Core Audio Format stream.");
                }

                result[i] = ChannelPrototype.FromStandardName(name);
                if (result[i] == ReferenceChannel.Unknown) {
                    throw new CorruptionException($"Unknown channel name: {name}.");
                }
                ObjectMapping[i] = id;
            }
            return result;
        }

        /// <summary>
        /// Fill the <see cref="objectMapping"/> array with the IDs of the <paramref name="objects"/> in the stream <paramref name="from"/> after the channel indices.
        /// </summary>
        void ParseObjectIDs(List<YAMLObject> objects, int from) {
            for (int i = 0, c = objects.Count; i < c; i++) {
                if (!(objects[i] is YAMLObject obj &&
                    obj.TryGetValue("ID", out object rawId) &&
                    rawId is string idString &&
                    int.TryParse(idString, out int id))) {
                    throw new CorruptionException("Invalid object mapping entry encountered.");
                }

                if (from < ObjectMapping.Length) {
                    ObjectMapping[from++] = id;
                } else {
                    throw new CorruptionException("More objects are mapped than the number of input streams.");
                }
            }

            if (from != ObjectMapping.Length) {
                throw new CorruptionException("Orphan object IDs found.");
            }
        }
    }
}
