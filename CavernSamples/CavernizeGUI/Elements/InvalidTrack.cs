using Cavern.Format.Common;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// An audio track's replacement when it failed to load.
    /// </summary>
    public class InvalidTrack : Track {
        /// <summary>
        /// An audio track's replacement when it failed to load.
        /// </summary>
        public InvalidTrack(string error, Codec codec, string language) {
            FormatHeader = $"{(string)MainWindow.language["InvTr"]}\n{error} {(string)MainWindow.language["Later"]}";
            Details = System.Array.Empty<(string, string)>();
            Codec = codec;
            Language = language;
        }
    }
}