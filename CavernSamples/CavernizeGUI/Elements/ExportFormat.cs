using System.Windows;

using Cavern.Format.Common;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A supported export format with mapping to FFmpeg.
    /// </summary>
    class ExportFormat(Codec codec, string ffName, int maxChannels, string description) {
        /// <summary>
        /// Cavern-compatible marking of the format.
        /// </summary>
        public Codec Codec { get; } = codec;

        /// <summary>
        /// Name of the format in FFmpeg.
        /// </summary>
        public string FFName { get; } = ffName;

        /// <summary>
        /// Maximum channel count of the format, either limited by the format itself or any first or third party integration.
        /// </summary>
        public int MaxChannels { get; } = maxChannels;

        /// <summary>
        /// Information about the format (full name, quality, size).
        /// </summary>
        public string Description { get; } = description;

        /// <summary>
        /// Displays the format's information for ComboBoxes.
        /// </summary>
        public override string ToString() => Description;

        /// <summary>
        /// All supported export formats.
        /// </summary>
        public static ExportFormat[] Formats {
            get {
                ResourceDictionary strings = Consts.Language.GetTrackStrings();
                return formats ??= [
                    new ExportFormat(Codec.AC3, "ac3", 6, (string)strings["C_AC3"]),
                    new ExportFormat(Codec.EnhancedAC3, "eac3", 8, (string)strings["CEAC3"]),
                    new ExportFormat(Codec.Opus, "libopus", 64, (string)strings["COpus"]),
                    new ExportFormat(Codec.FLAC, "flac", 8, (string)strings["CFLAC"]),
                    new ExportFormat(Codec.PCM_LE, "pcm_s16le", 64, (string)strings["CPCMI"]),
                    new ExportFormat(Codec.PCM_Float, "pcm_f32le", 64, (string)strings["CPCMF"]),
                    new ExportFormat(Codec.ADM_BWF, string.Empty, 128, (string)strings["CADMC"]),
                    new ExportFormat(Codec.ADM_BWF_Atmos, string.Empty, 128, (string)strings["CADMA"]),
                    new ExportFormat(Codec.LimitlessAudio, string.Empty, int.MaxValue, (string)strings["C_LAF"]),
                ];
            }
        }

        /// <summary>
        /// Cache for <see cref="Formats"/>, allocated on the first call.
        /// </summary>
        static ExportFormat[] formats;
    }
}