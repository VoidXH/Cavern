using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Sets height generation effect strength for the upconverter.
/// </summary>
sealed class EffectCommand : IntegerCommand {
    /// <inheritdoc/>
    public override string Name => "-effect";

    /// <inheritdoc/>
    public override string Alias => "-e";

    /// <inheritdoc/>
    public override string Help => "Sets height generation effect strength for the upconverter in percent.";

    /// <inheritdoc/>
    public override void Execute(int value, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "effect");
        }

        app.UpmixingSettings.Effect = value * .01f;
    }
}
