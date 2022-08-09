using Cavern.Format.Common;

namespace CavernizeGUI {
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
        /// Information about the format (full name, quality, size).
        /// </summary>
        public string Description { get; }

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