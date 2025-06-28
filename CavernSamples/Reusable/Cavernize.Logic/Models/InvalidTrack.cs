using Cavern.Format.Common;

using Cavernize.Logic.Language;

namespace Cavernize.Logic.Models;

/// <summary>
/// An audio track's replacement when it failed to load.
/// </summary>
public sealed class InvalidTrack : CavernizeTrack {
    /// <summary>
    /// An audio track's replacement when it failed to load.
    /// </summary>
    public InvalidTrack(string error, Codec codec, string language, TrackStrings strings) : base(strings) {
        FormatHeader = $"{strings.InvalidTrack}\n{error} {strings.Later}";
        Details = [];
        Codec = codec;
        Language = language;
    }
}
