using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Applies a set of FIR filters exported by QuickEQ on output channels.
/// </summary>
sealed class FilterCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-filter";

    /// <inheritdoc/>
    public override string Alias => "-filt";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Applies a set of FIR filters exported by QuickEQ on output channels.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "filtering");
        }

        if (!File.Exists(args[offset])) {
            throw new CommandException($"The file \"{args[offset]}\" does not exist.");
        }

        app.LoadRoomCorrection(args[offset], new ConversionStrings());
    }
}
