using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.MP4;
using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container {
    /// <summary>
    /// Reads ISO-BMFF, the MP4 container's data structure.
    /// </summary>
    public class MP4Reader : ContainerReader {
        /// <summary>
        /// The box containing the bytestreams of the <see cref="Tracks"/>.
        /// </summary>
        RawBox source;

        /// <summary>
        /// Minimal ISO-BMFF reader.
        /// </summary>
        public MP4Reader(Stream reader) : base(reader) { ReadSkeleton(); }

        /// <summary>
        /// Minimal ISO-BMFF reader.
        /// </summary>
        public MP4Reader(string path) : base(path) { ReadSkeleton(); }

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the <see cref="Tracks"/> array.</param>
        public override byte[] ReadNextBlock(int track) {
            throw new UnsupportedFeatureException("MP4");
        }

        /// <summary>
        /// Start the following reads from the selected timestamp.
        /// </summary>
        /// <returns>Position that was actually possible to seek to or -1 if the position didn't change.</returns>
        public override double Seek(double position) {
            throw new UnsupportedFeatureException("seek");
        }

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            while (reader.Position < reader.Length) {
                Box box = Box.Parse(reader);
                if (box.Header == metadataBox && box is NestedBox metadata) {
                    byte[] header = metadata[metadataHeaderBox]?.GetRawData(reader);
                    if (header == null) {
                        metadata.ThrowCorruption(metadataHeaderBox);
                    }
                    Duration = (double)header.ReadUInt32BE(16) / header.ReadUInt32BE(12);

                    List<Track> tracks = new List<Track>();
                    for (int i = 0; i < metadata.Contents.Length; i++) {
                        if (metadata.Contents[i] is TrackBox track) {
                            track.Track.Source = this;
                            tracks.Add(track.Track);
                        }
                    }
                    Tracks = tracks.ToArray();
                } else if (box is RawBox raw) {
                    source = raw;
                }
            }
        }
    }
}