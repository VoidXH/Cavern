using Cavern.Format.Common;

namespace CavernizeGUI.Elements {
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
                        new ExportFormat(Codec.Opus, "libopus", 64, "Opus (transparent, small size)"),
                        new ExportFormat(Codec.FLAC, "flac", 8, "FLAC (lossless, large size)"),
                        new ExportFormat(Codec.PCM_LE, "pcm_s16le", 64, "PCM integer (lossless, larger size)"),
                        new ExportFormat(Codec.PCM_Float, "pcm_f32le", 64, "PCM float (needless, largest size)"),
                        new ExportFormat(Codec.ADM_BWF, string.Empty, 128, "ADM Broadcast Wave Format (compact)"),
                        new ExportFormat(Codec.ADM_BWF_Atmos, string.Empty, 128, "ADM Broadcast Wave Format (Dolby Atmos)"),
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
        /// Maximum channel count of the format, either limited by the format itself or any first or third party integration.
        /// </summary>
        public int MaxChannels { get; }

        /// <summary>
        /// Information about the format (full name, quality, size).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// A supported export format with mapping to FFmpeg.
        /// </summary>
        public ExportFormat(Codec codec, string ffName, int maxChannels, string description) {
            Codec = codec;
            FFName = ffName;
            MaxChannels = maxChannels;
            Description = description;
        }

        /// <summary>
        /// Displays the format's information for ComboBoxes.
        /// </summary>
        public override string ToString() => Description;
    }
}