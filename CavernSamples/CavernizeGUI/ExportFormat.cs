using Cavern.Format.Common;

namespace CavernizeGUI {
    /// <summary>
    /// A supported export format with mapping to FFmpeg.
    /// </summary>
    class ExportFormat {
        /// <summary>
        /// All supported export formats.
        /// </summary>
        public static ExportFormat[] Formats {
            get {
                if (formats == null) {
                    formats = new ExportFormat[] {
                        new ExportFormat(Codec.Opus, "libopus", "Opus (transparent, small size)"),
                        new ExportFormat(Codec.PCM_LE, "pcm_s16le", "PCM integer (lossless, large size)"),
                        new ExportFormat(Codec.PCM_Float, "pcm_f32le", "PCM float (needless, largest size)"),
                        new ExportFormat(Codec.ADM_BWF, string.Empty, "ADM Broadcast Wave Format (studio)")
                    };
                }
                return formats;
            }
        }

        /// <summary>
        /// All supported export formats, cached for reuse.
        /// </summary>
        static ExportFormat[] formats;

        /// <summary>
        /// Cavern-compatible marking of the format.
        /// </summary>
        public Codec Codec { get; }

        /// <summary>
        /// Name of the format in FFmpeg.
        /// </summary>
        public string FFName { get; }

        /// <summary>
        /// Information about the format (full name, quality, size).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// A supported export format with mapping to FFmpeg.
        /// </summary>
        public ExportFormat(Codec codec, string ffName, string description) {
            Codec = codec;
            FFName = ffName;
            Description = description;
        }

        /// <summary>
        /// Displays the format's information for ComboBoxes.
        /// </summary>
        public override string ToString() => Description;
    }
}