using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Mutes the fixed bed channels.
/// </summary>
sealed class MuteBedCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-mute_bed";

    /// <inheritdoc/>
    public override string Alias => "-mb";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Mutes the fixed bed channels.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "muting");
        }

        app.SpecialRenderModeSettings.MuteBed = true;
    }
}
