using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Turns 7.1-creation from channel-based sources with less channels on or off.
/// </summary>
sealed class MatrixCommand : BooleanCommand {
    /// <inheritdoc/>
    public override string Name => "-matrix";

    /// <inheritdoc/>
    public override string Alias => "-mx";

    /// <inheritdoc/>
    public override string Help => "Turns 7.1-creation from channel-based sources with less channels on or off.";

    /// <inheritdoc/>
    public override void Execute(bool value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "matrixing");
        }

        app.UpmixingSettings.MatrixUpmixing = value;
    }
}
