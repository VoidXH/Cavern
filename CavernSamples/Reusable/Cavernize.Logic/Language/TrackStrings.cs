using Cavern.Format.Common;

using VoidX.WPF.Language;

namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used in the track selection UI.
/// </summary>
public class TrackStrings() : LanguageBase<TrackStrings>(new() {
    ["NoSup"] = "Format unsupported by Cavern",
    ["E3JOC"] = "Enhanced AC-3 with Joint Object Coding",
    ["ObTra"] = "Object-based audio track",
    ["ChTra"] = "Channel-based audio track",
    ["SouCh"] = "Source channels",
    ["MatBe"] = "Matrixed beds",
    ["MatOb"] = "Matrixed objects",
    ["SouBe"] = "Bed channels",
    ["SouDy"] = "Dynamic objects",
    ["Chans"] = "Channels",
    ["WiObj"] = "with objects",
    ["InvTr"] = "This track could not be decoded. The following error happened while decoding:",
    ["Later"] = "This might be fixed in later versions. Please try updating Cavernize, " +
        "and if the problem persists with the latest version, contact the developer at www.sbence.hu.",
    ["PCMFl"] = "PCM (floating point)",
    ["PCMLE"] = "PCM (integer)",
    ["C_AC3"] = "AC-3 (mediocre, supports SPDIF)",
    ["CEAC3"] = "Enhanced AC-3 (mediocre, supports HDMI ARC)",
    ["COpus"] = "Opus (transparent, small size)",
    ["CFLAC"] = "FLAC (lossless, large size)",
    ["CPCMF"] = "PCM, float (needless, largest size)",
    ["CPCMI"] = "PCM, integer (lossless, larger size)",
    ["CADMC"] = "ADM Broadcast Wave Format (compact)",
    ["CADMA"] = "ADM Broadcast Wave Format (Dolby Atmos)",
    ["CDAMF"] = "Dolby Atmos Master Format",
    ["C_LAF"] = "Limitless Audio Format",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<TrackStrings>[] GetTranslations() => [new TrackStringsHU(), new TrackStringsZH()];

    /// <summary>
    /// Translated names of supported codecs.
    /// </summary>
    public IReadOnlyDictionary<Codec, string> CodecNames => codecNames ??= new Dictionary<Codec, string> {
        { Codec.PCM_Float, this["PCMFl"] },
        { Codec.PCM_LE, this["PCMLE"] },
    };
    IReadOnlyDictionary<Codec, string> codecNames;

    /// <summary>
    /// Translated names and descriptions of export formats.
    /// </summary>
    public IReadOnlyDictionary<Codec, string> ExportFormats => exportFormats ??= new Dictionary<Codec, string> {
        { Codec.AC3, this["C_AC3"] },
        { Codec.EnhancedAC3, this["CEAC3"] },
        { Codec.Opus, this["COpus"] },
        { Codec.FLAC, this["CFLAC"] },
        { Codec.PCM_Float, this["CPCMF"] },
        { Codec.PCM_LE, this["CPCMI"] },
        { Codec.ADM_BWF, this["CADMC"] },
        { Codec.ADM_BWF_Atmos, this["CADMA"] },
        { Codec.DAMF, this["CDAMF"] },
        { Codec.LimitlessAudio, this["C_LAF"] },
    };
    IReadOnlyDictionary<Codec, string> exportFormats;
}
