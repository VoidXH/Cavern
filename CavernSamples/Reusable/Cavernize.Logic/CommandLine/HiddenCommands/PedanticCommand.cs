using Cavern.Format.Common;

using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Enables more thorough checks for conditions that are mandated by standards.
/// </summary>
class PedanticCommand : HiddenCommand {
    /// <inheritdoc/>
    public override string Name => "--pedantic";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Enables more thorough checks for conditions that are mandated by standards.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) => CavernFormatGlobal.Pedantic = true;
}
