using System.IO;

using Cavern.Format.Container.Matroska;

namespace Cavern.Format.Common {
    /// <summary>
    /// Video track metadata.
    /// </summary>
    public class TrackExtraVideo : TrackExtra {
        /// <summary>
        /// Uncropped width of a video frame.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Uncropped height of a video frame.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Frame update frequency.
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        ///  Used range of the available color values.
        /// </summary>
        public ColorRange ColorRange { get; set; }

        /// <summary>
        /// An optional codec header.
        /// </summary>
        public byte[] PrivateData { get; set; }

        /// <summary>
        /// An empty video track metadata.
        /// </summary>
        public TrackExtraVideo() { }

        /// <summary>
        /// Parse video metadata from a track's video metadata node.
        /// </summary>
        internal TrackExtraVideo(Stream reader, MatroskaTree videoMeta) {
            Width = (int)videoMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelWidth);
            Height = (int)videoMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelHeight);

            MatroskaTree color = videoMeta.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour);
            if (color != null) {
                long range = color.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_Range);
                if (range != -1) {
                    ColorRange = (ColorRange)range;
                }
            }
        }
    }

    /// <summary>
    /// Used range of the available color values.
    /// </summary>
    public enum ColorRange {
        /// <summary>
        /// Use the default color range of the format.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// 8 bits, 16-235.
        /// </summary>
        BroadcastRange = 1,
        /// <summary>
        /// Fully using all bits without clipping.
        /// </summary>
        FullRange = 2,
        /// <summary>
        /// Specified elsewhere.
        /// </summary>
        Other = 3
    }
}