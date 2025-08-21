using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Starts a render to this file.
/// </summary>
sealed class OutputCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-output";

    /// <inheritdoc/>
    public override string Alias => "-o";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Starts a render to this file.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) => app.RenderContent(args[offset]);
}
