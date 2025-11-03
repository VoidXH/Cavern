using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Forces 24-bit export for formats that support it, like WAV or LAF.
/// </summary>
sealed class Force24BitCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-force_24_bit";

    /// <inheritdoc/>
    public override string Alias => "-f24";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Forces 24-bit export for formats that support it, like WAV or LAF.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "forcing 24-bit");
        }

        app.SpecialRenderModeSettings.MuteBed = true;
    }
}
