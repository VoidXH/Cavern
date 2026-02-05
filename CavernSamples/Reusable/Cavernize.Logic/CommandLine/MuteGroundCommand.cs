using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Mutes objects that are not elevated.
/// </summary>
sealed class MuteGroundCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-mute_gnd";

    /// <inheritdoc/>
    public override string Alias => "-mg";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Mutes objects that are not elevated.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "muting");
        }

        app.RenderingSettings.MuteGround = true;
    }
}
