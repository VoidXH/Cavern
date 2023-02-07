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
            Details = $"{(string)MainWindow.language["InvTr"]} {error} {(string)MainWindow.language["Later"]}";
            Codec = codec;
            Language = language;
        }
    }
}