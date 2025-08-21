using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Sets generated object movement smoothness for the upconverter.
/// </summary>
sealed class SmoothnessCommand : IntegerCommand {
    /// <inheritdoc/>
    public override string Name => "-smooth";

    /// <inheritdoc/>
    public override string Alias => "-s";

    /// <inheritdoc/>
    public override string Help => "Sets generated object movement smoothness for the upconverter in percent.";

    /// <inheritdoc/>
    public override void Execute(int value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "smoothness");
        }

        app.UpmixingSettings.Smoothness = value * .01f;
    }
}
