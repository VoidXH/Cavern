using Cavern.Format.Common;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="TrackStrings"/> from Cavernize Avalonia's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="TrackStrings"/>.</param>
public sealed class DynamicTrackStrings(IReadOnlyDictionary<string, string> source) : TrackStrings {
    /// <inheritdoc/>
    public override string NotSupported => source["NoSup"];

    /// <inheritdoc/>
    public override string TypeEAC3JOC => source["E3JOC"];

    /// <inheritdoc/>
    public override string ObjectBasedTrack => source["ObTra"];

    /// <inheritdoc/>
    public override string ChannelBasedTrack => source["ChTra"];

    /// <inheritdoc/>
    public override string SourceChannels => source["SouCh"];

    /// <inheritdoc/>
    public override string MatrixedBeds => source["MatBe"];

    /// <inheritdoc/>
    public override string MatrixedObjects => source["MatOb"];

    /// <inheritdoc/>
    public override string BedChannels => source["SouBe"];

    /// <inheritdoc/>
    public override string DynamicObjects => source["SouDy"];

    /// <inheritdoc/>
    public override string Channels => source["Chans"];

    /// <inheritdoc/>
    public override string WithObjects => source["WiObj"];

    /// <inheritdoc/>
    public override string InvalidTrack => source["InvTr"];

    /// <inheritdoc/>
    public override string Later => source["Later"];

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetCodecNames() => new Dictionary<Codec, string> {
        { Codec.PCM_Float, source["PCM_Float"] },
        { Codec.PCM_LE, source["PCM_LE"] },
    };

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetExportFormats() => new Dictionary<Codec, string> {
        { Codec.AC3, source["C_AC3"] },
        { Codec.EnhancedAC3, source["CEAC3"] },
        { Codec.Opus, source["COpus"] },
        { Codec.FLAC, source["CFLAC"] },
        { Codec.PCM_Float, source["CPCMF"] },
        { Codec.PCM_LE, source["CPCMI"] },
        { Codec.ADM_BWF, source["CADMC"] },
        { Codec.ADM_BWF_Atmos, source["CADMA"] },
        { Codec.DAMF, source["CDAMF"] },
        { Codec.LimitlessAudio, source["C_LAF"] },
    };
}
