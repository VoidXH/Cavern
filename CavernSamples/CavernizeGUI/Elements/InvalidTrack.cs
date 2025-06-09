using Cavern.Format.Common;

using Cavernize.Logic.Models;
using CavernizeGUI.Language;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// An audio track's replacement when it failed to load.
    /// </summary>
    public class InvalidTrack : CavernizeTrack {
        /// <summary>
        /// An audio track's replacement when it failed to load.
        /// </summary>
        public InvalidTrack(string error, Codec codec, string language, DynamicTrackStrings strings) : base(strings) {
            FormatHeader = $"{(string)MainWindow.language["InvTr"]}\n{error} {(string)MainWindow.language["Later"]}";
            Details = [];
            Codec = codec;
            Language = language;
        }
    }
}
