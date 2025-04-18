using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.MP4;
using Cavern.Format.Utilities;
using Cavern.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container {
    /// <summary>
    /// Reads ISO-BMFF, the MP4 container's data structure.
    /// </summary>
    public class MP4Reader : ContainerReader {
        /// <inheritdoc/>
        public override Common.Container Type => Common.Container.MP4;

        /// <summary>
        /// Minimal ISO-BMFF reader.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        public MP4Reader(Stream reader) : this(reader, 0) { }

        /// <summary>
        /// Minimal ISO-BMFF reader.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        /// <param name="skippedSyncWord">The sync word from which the format is detected, which already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public MP4Reader(Stream reader, uint skippedSyncWord) : base(reader) => ReadSkeleton(skippedSyncWord);

        /// <summary>
        /// Minimal ISO-BMFF reader.
        /// </summary>
        public MP4Reader(string path) : base(path) => ReadSkeleton(0);

        /// <summary>
        /// The following block of the track is rendered and available.
        /// </summary>
        public override bool IsNextBlockAvailable(int track) {
            MP4Track source = (MP4Track)Tracks[track];
            return source.map.Length != source.nextSample;
        }

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the Tracks array.</param>
        public override byte[] ReadNextBlock(int track) {
            MP4Track source = (MP4Track)Tracks[track];
            if (source.map.Length != source.nextSample) {
                reader.Position = (long)source.map[source.nextSample].offset;
                return reader.ReadBytes(source.map[source.nextSample++].length);
            }
            return null;
        }

        /// <summary>
        /// Start the following reads from the selected timestamp.
        /// </summary>
        /// <returns>Position that was actually possible to seek to or -1 if the position didn't change.</returns>
        public override double Seek(double position) {
            double audioTime = -1, mainTime = -1;
            for (int i = 0; i < Tracks.Length; i++) {
                MP4Track source = (MP4Track)Tracks[i];
                (long index, double time) = source.GetSample(position);
                if (index != -1) {
                    if (mainTime == -1) {
                        mainTime = time;
                    }
                    if (audioTime == -1 && Tracks[i].Format.IsSupportedAudio()) {
                        audioTime = time;
                    }
                    source.nextSample = index;
                }
            }
            return audioTime != -1 ? audioTime : mainTime;
        }

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        /// <param name="skippedSyncWord">The sync word from which the format is detected, which already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        void ReadSkeleton(uint skippedSyncWord) {
            while (reader.Position < reader.Length) {
                Box box;
                if (skippedSyncWord == 0) {
                    box = Box.Parse(reader);
                } else {
                    box = Box.Parse(reader, skippedSyncWord.ReverseEndianness() - 8, fileTypeBox);
                    skippedSyncWord = 0;
                }

                if (box.Header == metadataBox && box is NestedBox metadata) {
                    byte[] header = metadata[metadataHeaderBox]?.GetRawData(reader) ??
                        throw new MissingElementException(metadataHeaderBox.ToFourCC());
                    uint timeScale = header.ReadUInt32BE(12); // Duration and edit lists use this many units per second
                    Duration = (double)header.ReadUInt32BE(16) / timeScale;

                    List<Track> tracks = new List<Track>();
                    for (int i = 0; i < metadata.Contents.Length; i++) {
                        if (metadata.Contents[i] is TrackBox track) {
                            track.Track.Override(this, tracks.Count);
                            tracks.Add(track.Track);
                        }
                    }
                    Tracks = tracks.ToArray();
                }
            }
        }
    }
}