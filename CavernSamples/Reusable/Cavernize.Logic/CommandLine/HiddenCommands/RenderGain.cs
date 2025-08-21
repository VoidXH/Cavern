using Cavern.Utilities;

using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Adds additional gain to renders.
/// </summary>
sealed class RenderGain : UnsafeCommand {
    /// <inheritdoc/>
    public override string Name => "--render-gain";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Applies additional gain to the content volume in decibels.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (!QMath.TryParseFloat(args[offset], out float attenuation)) {
            throw new CommandException($"The provided render gain ({args[offset]}) is not a number.");
        }
        app.RenderGain = QMath.DbToGain(attenuation);
    }
}
