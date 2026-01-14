using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Move elevated channels inward by this percentage. 0 is at the sides, 1 is at the center.
/// </summary>
sealed class HeightSqueezeCommand : UnsafeCommand {
    /// <inheritdoc/>
    public override string Name => "--height-squeeze";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Move elevated channels inward by this percentage.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (!int.TryParse(args[offset], out int percent)) {
            throw new CommandException($"The provided squeeze percentage ({args[offset]}) is not a whole number.");
        }
        RenderTarget.HeightSqueeze = percent * .01f;
    }
}
