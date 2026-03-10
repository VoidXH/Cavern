using System.IO;

using Cavern.Format.Common.Metadata;
using Cavern.Format.Common.Metadata.Enums;

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
        internal MatroskaTrackExtraVideo(Stream reader, MatroskaTree videoMeta) {
            Width = (uint)videoMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelWidth);
            Height = (uint)videoMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelHeight);

            MatroskaTree color = videoMeta.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour);
            if (color != null) {
                ColorMetadata = new ColorMetadata();
                long read = color.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_Range);
                if (read != -1) {
                    ColorMetadata.ColorRange = (ColorRange)read;
                }
                read = color.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_MaxCLL);
                if (read != -1) {
                    ColorMetadata.MaxCLL = (uint)read;
                }
                read = color.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_MaxFALL);
                if (read != -1) {
                    ColorMetadata.MaxFALL = (uint)read;
                }
            }
        }
    }
}
