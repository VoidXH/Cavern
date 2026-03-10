using Cavern.Format.Common.Metadata.Enums;

namespace Cavern.Format.Common.Metadata {
    /// <summary>
    /// Color-related values in video track metadata.
    /// </summary>
    public class ColorMetadata {
        /// <summary>
        /// Used range of the available color values.
        /// </summary>
        public ColorRange ColorRange { get; set; }

        /// <summary>
        /// Maximum content light level.
        /// </summary>
        public uint MaxCLL { get; set; }

        /// <summary>
        /// Maximum frame-average light level.
        /// </summary>
        public uint MaxFALL { get; set; }
    }
}
