using System.Windows;

using Cavern.Format.Common;

using CavernizeGUI.Consts;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A supported export format with mapping to FFmpeg.
    /// </summary>
    class ExportFormat {
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

        /// <summary>
        /// All supported export formats.
        /// </summary>
        public static ExportFormat[] Formats {
            get {
                ResourceDictionary strings = Language.GetTrackStrings();
                return formats ??= new[] {
                    new ExportFormat(Codec.Opus, "libopus", 64, (string)strings["COpus"]),
                    new ExportFormat(Codec.FLAC, "flac", 8, (string)strings["CFLAC"]),
                    new ExportFormat(Codec.PCM_LE, "pcm_s16le", 64, (string)strings["CPCMI"]),
                    new ExportFormat(Codec.PCM_Float, "pcm_f32le", 64, (string)strings["CPCMF"]),
                    new ExportFormat(Codec.ADM_BWF, string.Empty, 128, (string)strings["CADMC"]),
                    new ExportFormat(Codec.ADM_BWF_Atmos, string.Empty, 128, (string)strings["CADMA"]),
                    new ExportFormat(Codec.LimitlessAudio, string.Empty, int.MaxValue, (string)strings["C_LAF"]),
                };
            }
        }

        /// <summary>
        /// Cache for <see cref="Formats"/>, allocated on the first call.
        /// </summary>
        static ExportFormat[] formats;
    }
}