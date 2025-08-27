using Cavern.Format.Common;

using Cavernize.Logic.Language;

namespace Cavernize.Logic.Models;

/// <summary>
/// A supported export format with mapping to FFmpeg.
/// </summary>
public sealed class ExportFormat(Codec codec, string ffName, int maxChannels, string description) {
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
    /// Get all supported export formats in the user's <paramref name="language"/>.
    /// </summary>
    public static ExportFormat[] GetFormats(TrackStrings language) {
        IReadOnlyDictionary<Codec, string> strings = language.ExportFormats;
        return formats ??= [
            new ExportFormat(Codec.AC3, "ac3", 6, strings[Codec.AC3]),
            new ExportFormat(Codec.EnhancedAC3, "eac3", 8, strings[Codec.EnhancedAC3]),
            new ExportFormat(Codec.Opus, "libopus", 64, strings[Codec.Opus]),
            new ExportFormat(Codec.FLAC, "flac", 8, strings[Codec.FLAC]),
            new ExportFormat(Codec.PCM_LE, "pcm_s16le", 64, strings[Codec.PCM_LE]),
            new ExportFormat(Codec.PCM_Float, "pcm_f32le", 64, strings[Codec.PCM_Float]),
            new ExportFormat(Codec.ADM_BWF, string.Empty, 128, strings[Codec.ADM_BWF]),
            new ExportFormat(Codec.ADM_BWF_Atmos, string.Empty, 128, strings[Codec.ADM_BWF_Atmos]),
            new ExportFormat(Codec.DAMF, string.Empty, 128, strings[Codec.DAMF]),
            new ExportFormat(Codec.LimitlessAudio, string.Empty, int.MaxValue, strings[Codec.LimitlessAudio]),
        ];
    }

    /// <summary>
    /// Cache for <see cref="Formats"/>, allocated on the first call.
    /// </summary>
    static ExportFormat[] formats;
}
