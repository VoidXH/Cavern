using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.CommandLine;
/// <summary>
/// Selects a standard channel layout.
/// </summary>
sealed class TargetCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-target";

    /// <inheritdoc/>
    public override string Alias => "-t";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Selects a standard channel layout for rendering the content in.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "render target");
        }

        for (int i = 0; i < RenderTarget.Targets.Length; i++) {
            string name = RenderTarget.Targets[i].Name;
            if (args[offset].Equals(name)) {
                app.RenderTarget = RenderTarget.Targets[i];
                return;
            }

            int index = name.IndexOf(' ');
            if (index != -1) {
                name = name[..index];
                if (args[offset].Equals(name)) {
                    app.RenderTarget = RenderTarget.Targets[i];
                    return;
                }
            }

            name = name.Replace(".", string.Empty);
            if (args[offset].Equals(name)) {
                app.RenderTarget = RenderTarget.Targets[i];
                return;
            }
        }

        string valids = string.Join<RenderTarget>(", ", RenderTarget.Targets);
        throw new CommandException($"Invalid rendering target ({args[offset]}). Valid options are: {valids}.");
    }
}
