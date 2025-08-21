using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Opens a content file.
/// </summary>
sealed class InputCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-input";

    /// <inheritdoc/>
    public override string Alias => "-i";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Opens a content file.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (!File.Exists(args[offset])) {
            throw new CommandException($"The file \"{args[offset]}\" does not exist.");
        }
        app.OpenContent(args[offset]);
    }
}
