using Cavern.Format.Common;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="TrackStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="TrackStrings"/></param>
public sealed class DynamicTrackStrings(IReadOnlyDictionary<string, string> source) : TrackStrings {
    /// <inheritdoc/>
    public override string NotSupported => (string)source["NoSup"];

    /// <inheritdoc/>
    public override string TypeEAC3JOC => (string)source["E3JOC"];

    /// <inheritdoc/>
    public override string ObjectBasedTrack => (string)source["ObTra"];

    /// <inheritdoc/>
    public override string ChannelBasedTrack => (string)source["ChTra"];

    /// <inheritdoc/>
    public override string SourceChannels => (string)source["SouCh"];

    /// <inheritdoc/>
    public override string MatrixedBeds => (string)source["MatBe"];

    /// <inheritdoc/>
    public override string MatrixedObjects => (string)source["MatOb"];

    /// <inheritdoc/>
    public override string BedChannels => (string)source["SouBe"];

    /// <inheritdoc/>
    public override string DynamicObjects => (string)source["SouDy"];

    /// <inheritdoc/>
    public override string Channels => (string)source["Chans"];

    /// <inheritdoc/>
    public override string WithObjects => (string)source["WiObj"];

    /// <inheritdoc/>
    public override string InvalidTrack => (string)source["InvTr"];

    /// <inheritdoc/>
    public override string Later => (string)source["Later"];

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetCodecNames() => new Dictionary<Codec, string> {
        { Codec.PCM_Float, (string)source["PCM_Float"] },
        { Codec.PCM_LE, (string)source["PCM_LE"] },
    };

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetExportFormats() => new Dictionary<Codec, string> {
        { Codec.AC3, (string)source["C_AC3"] },
        { Codec.EnhancedAC3, (string)source["CEAC3"] },
        { Codec.Opus, (string)source["COpus"] },
        { Codec.FLAC, (string)source["CFLAC"] },
        { Codec.PCM_Float, (string)source["CPCMF"] },
        { Codec.PCM_LE, (string)source["CPCMI"] },
        { Codec.ADM_BWF, (string)source["CADMC"] },
        { Codec.ADM_BWF_Atmos, (string)source["CADMA"] },
        { Codec.DAMF, (string)source["CDAMF"] },
        { Codec.LimitlessAudio, (string)source["C_LAF"] },
    };
}
