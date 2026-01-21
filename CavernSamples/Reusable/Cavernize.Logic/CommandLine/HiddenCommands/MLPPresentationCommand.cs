using Cavern.Format.Renderers;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Force the selected Meridian Lossless Packing presentation index.
/// </summary>
sealed class MLPPresentationCommand : UnsafeCommand {
    /// <inheritdoc/>
    public override string Name => "--mlp-presentation";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Force the selected MLP presentation index.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (!int.TryParse(args[offset], out int presentation) || presentation < 0) {
            throw new CommandException($"The selected presentation ({args[offset]}) is not a non-negative whole number.");
        }
        MeridianLosslessPackingRenderer.ForcedPresentation = presentation;
    }
}
