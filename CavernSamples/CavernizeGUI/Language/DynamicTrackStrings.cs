using System.Collections.Generic;
using System.Windows;

using Cavern.Format.Common;
using Cavern.WPF.Utils;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="TrackStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="TrackStrings"/></param>
public sealed class DynamicTrackStrings(ResourceDictionary source) : TrackStrings {
    /// <inheritdoc/>
    public override string NotSupported => source.TryGet("NoSup", base.NotSupported);

    /// <inheritdoc/>
    public override string TypeEAC3JOC => source.TryGet("E3JOC", base.TypeEAC3JOC);

    /// <inheritdoc/>
    public override string ObjectBasedTrack => source.TryGet("ObTra", base.ObjectBasedTrack);

    /// <inheritdoc/>
    public override string ChannelBasedTrack => source.TryGet("ChTra", base.ChannelBasedTrack);

    /// <inheritdoc/>
    public override string SourceChannels => source.TryGet("SouCh", base.SourceChannels);

    /// <inheritdoc/>
    public override string MatrixedBeds => source.TryGet("MatBe", base.MatrixedBeds);

    /// <inheritdoc/>
    public override string MatrixedObjects => source.TryGet("MatOb", base.MatrixedObjects);

    /// <inheritdoc/>
    public override string BedChannels => source.TryGet("SouBe", base.BedChannels);

    /// <inheritdoc/>
    public override string DynamicObjects => source.TryGet("SouDy", base.DynamicObjects);

    /// <inheritdoc/>
    public override string Channels => source.TryGet("Chans", base.Channels);

    /// <inheritdoc/>
    public override string WithObjects => source.TryGet("WiObj", base.WithObjects);

    /// <inheritdoc/>
    public override string InvalidTrack => source.TryGet("InvTr", base.InvalidTrack);

    /// <inheritdoc/>
    public override string Later => source.TryGet("Later", base.Later);

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetCodecNames() => (string)source["NoSup"] != null ?
        new Dictionary<Codec, string> {
            { Codec.PCM_Float, (string)source["PCM_Float"] },
            { Codec.PCM_LE, (string)source["PCM_LE"] },
        } :
        base.GetCodecNames();

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<Codec, string> GetExportFormats() => (string)source["NoSup"] != null ?
        new Dictionary<Codec, string> {
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
        } :
        base.GetCodecNames();
}
