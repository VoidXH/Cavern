using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Turns height generation from regular content on or off, up to 7.1.
/// </summary>
sealed class UpconvertCommand : BooleanCommand {
    /// <inheritdoc/>
    public override string Name => "-upconvert";

    /// <inheritdoc/>
    public override string Alias => "-u";

    /// <inheritdoc/>
    public override string Help => "Turns height generation from regular content up to 7.1 on or off.";

    /// <inheritdoc/>
    public override void Execute(bool value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "upconversion");
        }

        app.UpmixingSettings.Cavernize = value;
    }
}
