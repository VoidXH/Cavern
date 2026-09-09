using System.Collections.Generic;

using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;

namespace Cavern.Format.Common.Metadata {
    /// <summary>
    /// Exports Audio Definition Model (ADM) metadata to human-readable format.
    /// </summary>
    public static class ADMMetadataExporter {
        /// <summary>
        /// Generates human-readable metadata from an <see cref="AudioDefinitionModel"/>.
        /// </summary>
        /// <param name="adm">The ADM object to read metadata from.</param>
        /// <returns>A <see cref="ReadableMetadata"/> containing all ADM sections, or null if ADM is null.</returns>
        public static ReadableMetadata GenerateMetadata(AudioDefinitionModel adm) {
            if (adm == null) {
                return null;
            }

            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader>();

            // Programs
            if (adm.Programs.Count > 0) {
                List<ReadableMetadataField> programmeFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.Programs.Count; i++) {
                    ADMProgramme prog = adm.Programs[i];
                    programmeFields.Add(new ReadableMetadataField($"Programme {i + 1} ID", "Audio programme identifier", prog.ID));
                    programmeFields.Add(new ReadableMetadataField($"Programme {i + 1} Name", "Audio programme name", prog.Name));
                    programmeFields.Add(new ReadableMetadataField($"Programme {i + 1} Length", "Audio programme duration", prog.Length.ToString()));
                    programmeFields.Add(new ReadableMetadataField($"Programme {i + 1} Contents", "Referenced audio contents", string.Join(", ", prog.Contents)));
                }
                headers.Add(new ReadableMetadataHeader("Audio Programmes", programmeFields));
            }

            // Contents
            if (adm.Contents.Count > 0) {
                List<ReadableMetadataField> contentFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.Contents.Count; i++) {
                    ADMContent content = adm.Contents[i];
                    contentFields.Add(new ReadableMetadataField($"Content {i + 1} ID", "Audio content identifier", content.ID));
                    contentFields.Add(new ReadableMetadataField($"Content {i + 1} Name", "Audio content name", content.Name));
                    contentFields.Add(new ReadableMetadataField($"Content {i + 1} Objects", "Referenced audio objects", string.Join(", ", content.Objects)));
                }
                headers.Add(new ReadableMetadataHeader("Audio Contents", contentFields));
            }

            // Objects
            if (adm.Objects.Count > 0) {
                List<ReadableMetadataField> objectFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.Objects.Count; i++) {
                    ADMObject obj = adm.Objects[i];
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} ID", "Audio object identifier", obj.ID));
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} Name", "Audio object name", obj.Name));
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} Offset", "Object start time", obj.Offset.ToString()));
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} Length", "Object duration", obj.Length.ToString()));
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} Pack Format", "Referenced pack format", obj.PackFormat));
                    objectFields.Add(new ReadableMetadataField($"Object {i + 1} Tracks", "Referenced tracks", string.Join(", ", obj.Tracks)));
                }
                headers.Add(new ReadableMetadataHeader("Audio Objects", objectFields));
            }

            // Pack Formats
            if (adm.PackFormats.Count > 0) {
                List<ReadableMetadataField> packFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.PackFormats.Count; i++) {
                    ADMPackFormat pack = adm.PackFormats[i];
                    packFields.Add(new ReadableMetadataField($"Pack Format {i + 1} ID", "Audio pack format identifier", pack.ID));
                    packFields.Add(new ReadableMetadataField($"Pack Format {i + 1} Name", "Audio pack format name", pack.Name));
                    packFields.Add(new ReadableMetadataField($"Pack Format {i + 1} Type", "Pack format type", pack.Type.ToString()));
                    packFields.Add(new ReadableMetadataField($"Pack Format {i + 1} Channel Formats", "Referenced channel formats", string.Join(", ", pack.ChannelFormats)));
                }
                headers.Add(new ReadableMetadataHeader("Audio Pack Formats", packFields));
            }

            // Channel Formats (movements)
            if (adm.ChannelFormats.Count > 0) {
                List<ReadableMetadataField> channelFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.ChannelFormats.Count; i++) {
                    ADMChannelFormat ch = adm.ChannelFormats[i];
                    channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} ID", "Audio channel format identifier", ch.ID));
                    channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} Name", "Audio channel format name", ch.Name));
                    channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} Type", "Channel format type", ch.Type.ToString()));
                    channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} Blocks", "Number of position blocks", ch.Blocks.Count.ToString()));
                    if (ch.Blocks.Count > 0) {
                        ADMBlockFormat firstBlock = ch.Blocks[0];
                        channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} First Position", "First position (X, Y, Z)", $"({firstBlock.Position.X:F3}, {firstBlock.Position.Y:F3}, {firstBlock.Position.Z:F3})"));
                        channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} First Offset", "First block offset", firstBlock.Offset.ToString()));
                        channelFields.Add(new ReadableMetadataField($"Channel Format {i + 1} First Duration", "First block duration", firstBlock.Duration.ToString()));
                    }
                }
                headers.Add(new ReadableMetadataHeader("Audio Channel Formats", channelFields));
            }

            // Tracks
            if (adm.Tracks.Count > 0) {
                List<ReadableMetadataField> trackFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.Tracks.Count; i++) {
                    ADMTrack track = adm.Tracks[i];
                    trackFields.Add(new ReadableMetadataField($"Track {i + 1} ID", "Audio track identifier", track.ID));
                    trackFields.Add(new ReadableMetadataField($"Track {i + 1} Bit Depth", "Track bit depth", track.Bits.ToString()));
                    trackFields.Add(new ReadableMetadataField($"Track {i + 1} Sample Rate", "Track sample rate", track.SampleRate.ToString()));
                    trackFields.Add(new ReadableMetadataField($"Track {i + 1} Track Format", "Referenced track format", track.TrackFormat));
                    trackFields.Add(new ReadableMetadataField($"Track {i + 1} Pack Format", "Referenced pack format", track.PackFormat));
                }
                headers.Add(new ReadableMetadataHeader("Audio Tracks", trackFields));
            }

            // Track Formats
            if (adm.TrackFormats.Count > 0) {
                List<ReadableMetadataField> trackFormatFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.TrackFormats.Count; i++) {
                    ADMTrackFormat tf = adm.TrackFormats[i];
                    trackFormatFields.Add(new ReadableMetadataField($"Track Format {i + 1} ID", "Audio track format identifier", tf.ID));
                    trackFormatFields.Add(new ReadableMetadataField($"Track Format {i + 1} Name", "Audio track format name", tf.Name));
                    trackFormatFields.Add(new ReadableMetadataField($"Track Format {i + 1} Format", "Track codec format", tf.Format.ToString()));
                    trackFormatFields.Add(new ReadableMetadataField($"Track Format {i + 1} Stream Format", "Referenced stream format", tf.StreamFormat));
                }
                headers.Add(new ReadableMetadataHeader("Audio Track Formats", trackFormatFields));
            }

            // Stream Formats
            if (adm.StreamFormats.Count > 0) {
                List<ReadableMetadataField> streamFields = new List<ReadableMetadataField>();
                for (int i = 0; i < adm.StreamFormats.Count; i++) {
                    ADMStreamFormat sf = adm.StreamFormats[i];
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} ID", "Audio stream format identifier", sf.ID));
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} Name", "Audio stream format name", sf.Name));
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} Format", "Stream codec format", sf.Format.ToString()));
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} Channel Format", "Referenced channel format", sf.ChannelFormat));
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} Pack Format", "Referenced pack format", sf.PackFormat));
                    streamFields.Add(new ReadableMetadataField($"Stream Format {i + 1} Track Format", "Referenced track format", sf.TrackFormat));
                }
                headers.Add(new ReadableMetadataHeader("Audio Stream Formats", streamFields));
            }

            return new ReadableMetadata(headers);
        }
    }
}
