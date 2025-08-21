using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Virtualizes and downmixes elevated objects to ground-only layouts.
/// </summary>
sealed class SpeakerVirtualizerCommand : BooleanCommand {
    /// <inheritdoc/>
    public override string Name => "-speaker_virt";

    /// <inheritdoc/>
    public override string Alias => "-sv";

    /// <inheritdoc/>
    public override string Help => "Virtualizes and downmixes elevated objects to ground-only layouts.";

    /// <inheritdoc/>
    public override void Execute(bool value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "speaker virtualization");
        }

        app.SpecialRenderModeSettings.SpeakerVirtualizer = value;
    }
}
