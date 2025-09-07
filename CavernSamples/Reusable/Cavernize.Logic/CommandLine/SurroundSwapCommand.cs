using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Swaps the side and rear surround outputs with each other.
/// </summary>
sealed class SurroundSwapCommand : BooleanCommand {
    /// <inheritdoc/>
    public override string Name => "-surround_swap";

    /// <inheritdoc/>
    public override string Alias => "-ss";

    /// <inheritdoc/>
    public override string Help => "Swaps the side and rear surround outputs with each other.";

    /// <inheritdoc/>
    public override void Execute(bool value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "surround swap");
        }

        app.SurroundSwap = value;
    }
}
