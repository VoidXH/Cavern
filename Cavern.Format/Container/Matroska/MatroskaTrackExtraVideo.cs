using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Video track metadata with fields that are only contained in Matroska files.
    /// </summary>
    public class MatroskaTrackExtraVideo : TrackExtraVideo {
        /// <summary>
        /// Elements that extend the track format, other than <see cref="TrackExtraVideo.PrivateData"/>.
        /// This is not decoded, just transfered.
        /// </summary>
        public byte[] BlockAdditionMapping { get; set; }

        /// <summary>
        /// Parse video metadata from a Matroska track's video metadata node.
        /// </summary>
        internal MatroskaTrackExtraVideo(Stream reader, MatroskaTree videoMeta) : base(reader, videoMeta) { }
    }
}